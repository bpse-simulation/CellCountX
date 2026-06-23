import json

try:
    # Python 3.8+
    from importlib.metadata import version, PackageNotFoundError
except ImportError:
    # Python <3.8
    from importlib_metadata import version, PackageNotFoundError

try:
    ver = version("cellpose")
except PackageNotFoundError:
    ver = "unknown"

print(json.dumps({
    "cellpose_version": ver
}))
