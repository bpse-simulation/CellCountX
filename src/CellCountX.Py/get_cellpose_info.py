import json

try:
    # Python 3.8+
    from importlib.metadata import version, PackageNotFoundError
except ImportError:
    # Python <3.8
    from importlib_metadata import version, PackageNotFoundError

# Cellpose version
try:
    ver = version("cellpose")
except PackageNotFoundError:
    ver = "unknown"

gpu_available = False
gpu_backend = "none"

try:
    import torch

    # CUDA (NVIDIA)
    if torch.cuda.is_available():
        gpu_available = True
        gpu_backend = "cuda"

    # macOS MPS
    elif hasattr(torch.backends, "mps") and torch.backends.mps.is_available():
        gpu_available = True
        gpu_backend = "mps"

    # AMD ROCm (Linux / Windows)
    elif hasattr(torch.version, "hip") and torch.version.hip:
        gpu_available = True
        gpu_backend = "rocm"

    # AMD DirectML (Windows)
    elif hasattr(torch.backends, "dml") and torch.backends.dml.is_available():
        gpu_available = True
        gpu_backend = "directml"

except Exception:
    gpu_available = False
    gpu_backend = "none"

print(json.dumps({
    "cellpose_version": ver,
    "gpu_available": gpu_available,
    "gpu_backend": gpu_backend
}))
