import numpy as np
from skimage.measure import regionprops
import pickle

# ---------------------------------------------------------
# RF モデル読み込み
# ---------------------------------------------------------
with open("rf_keep_adherent.pkl", "rb") as f:
    RF_MODEL = pickle.load(f)

# ---------------------------------------------------------
# 特徴量抽出 + RF 推論
# ---------------------------------------------------------
def extract_features_and_predict(img, labels, clf):
    labels = labels.astype(np.int32)
    props = regionprops(labels, intensity_image=img)

    feature_list = []
    object_ids = []

    for p in props:
        area = float(p.area)
        mean_intensity = float(p.mean_intensity)
        variance = float(np.var(p.intensity_image))

        if p.perimeter == 0:
            circularity = 1.0
        else:
            circularity = float(4 * np.pi * area / (p.perimeter ** 2))

        feature_list.append([area, circularity, mean_intensity, variance])
        object_ids.append(p.label)

    if len(feature_list) == 0:
        return [], [], [], np.zeros_like(labels), np.zeros_like(labels)

    X = np.array(feature_list, dtype=np.float32)
    pred = clf.predict(X)

    keep_mask = np.zeros_like(labels)
    remove_mask = np.zeros_like(labels)

    for obj_id, keep in zip(object_ids, pred):
        if keep == 1:
            keep_mask[labels == obj_id] = obj_id
        else:
            remove_mask[labels == obj_id] = obj_id

    return object_ids, feature_list, pred, keep_mask, remove_mask

# ---------------------------------------------------------
# ラベル付け直し
# ---------------------------------------------------------
def relabel_sequential(mask):
    unique_labels = np.unique(mask)
    unique_labels = unique_labels[unique_labels != 0]

    new_mask = np.zeros_like(mask)
    for new_id, old_id in enumerate(unique_labels, start=1):
        new_mask[mask == old_id] = new_id

    return new_mask
