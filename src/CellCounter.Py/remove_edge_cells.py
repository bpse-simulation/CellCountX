# remove_edge_cells.py
import numpy as np

def get_edge_labels(label_img, use_top, use_bottom, use_left, use_right, margin=2):
    H, W = label_img.shape
    edge_labels = set()

    if use_top:
        edge_labels.update(np.unique(label_img[0:margin, :]))
    if use_bottom:
        edge_labels.update(np.unique(label_img[H-margin:H, :]))
    if use_left:
        edge_labels.update(np.unique(label_img[:, 0:margin]))
    if use_right:
        edge_labels.update(np.unique(label_img[:, W-margin:W]))

    edge_labels.discard(0)
    return edge_labels


def remove_edge_cells(label_img, use_top=True, use_bottom=True, use_left=True, use_right=True, margin=2):
    """
    画像端の4方向それぞれで除去するかどうかを設定できる。
    """
    edge_labels = get_edge_labels(label_img, use_top, use_bottom, use_left, use_right, margin)

    cleaned = label_img.copy()
    for lbl in edge_labels:
        cleaned[cleaned == lbl] = 0

    return cleaned, edge_labels
