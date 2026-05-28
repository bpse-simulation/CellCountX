import pandas as pd
import numpy as np
import pickle
import json
from sklearn.ensemble import RandomForestClassifier
from sklearn.model_selection import train_test_split
from sklearn.metrics import classification_report, confusion_matrix

# ---------------------------------------------------------
# 設定
# ---------------------------------------------------------
CSV_PATH = "all_objects_features.csv"   # extract_features_folder.py が出力した CSV
MODEL_PATH = "rf_keep_adherent.pkl"     # pickle 保存
JSON_PATH = "rf_keep_adherent.json"     # JSON 保存

# ---------------------------------------------------------
# 1. CSV 読み込み
# ---------------------------------------------------------
df = pd.read_csv(CSV_PATH)

# 必要な特徴量
feature_cols = ["area", "circularity", "mean_intensity", "variance"]
X = df[feature_cols].values

# ラベル（あなたが付ける）
# 0 = 非接着（除外）
# 1 = 接着（残す）
# 2 = 細胞以外（除外）
y_raw = df["label"].values

# ---------------------------------------------------------
# 2. 接着 vs 非接着＋ゴミ の 2 クラス分類に変換
# ---------------------------------------------------------
y = np.where(y_raw == 1, 1, 0)

# ---------------------------------------------------------
# 3. 学習データとテストデータに分割
# ---------------------------------------------------------
X_train, X_test, y_train, y_test = train_test_split(
    X, y, test_size=0.2, random_state=42, stratify=y
)

# ---------------------------------------------------------
# 4. Random Forest モデル作成
# ---------------------------------------------------------
clf = RandomForestClassifier(
    n_estimators=300,
    max_depth=None,
    random_state=42,
    class_weight="balanced"
)

clf.fit(X_train, y_train)

# ---------------------------------------------------------
# 5. 評価
# ---------------------------------------------------------
pred = clf.predict(X_test)

print("=== Confusion Matrix ===")
print(confusion_matrix(y_test, pred))

print("\n=== Classification Report ===")
print(classification_report(y_test, pred, digits=4))

# ---------------------------------------------------------
# 6. 特徴量重要度
# ---------------------------------------------------------
print("\n=== Feature Importance ===")
for name, score in zip(feature_cols, clf.feature_importances_):
    print(f"{name:15s}: {score:.4f}")

# ---------------------------------------------------------
# 7. モデル保存（pickle）
# ---------------------------------------------------------
with open(MODEL_PATH, "wb") as f:
    pickle.dump(clf, f)

print(f"\nSaved pickle model to {MODEL_PATH}")

# ---------------------------------------------------------
# 8. モデル保存（JSON）
# ---------------------------------------------------------
def tree_to_dict(tree):
    tree_ = tree.tree_
    return {
        "children_left": tree_.children_left.tolist(),
        "children_right": tree_.children_right.tolist(),
        "feature": tree_.feature.tolist(),
        "threshold": tree_.threshold.tolist(),
        "value": tree_.value.squeeze(axis=1).tolist()
    }

forest_dict = {
    "n_estimators": len(clf.estimators_),
    "trees": [tree_to_dict(estimator) for estimator in clf.estimators_]
}

with open(JSON_PATH, "w") as f:
    json.dump(forest_dict, f)

print(f"Saved JSON model to {JSON_PATH}")
