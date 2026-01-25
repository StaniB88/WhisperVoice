import json
import warnings
warnings.filterwarnings("ignore")

try:
    import torch
    info = {
        'pytorchVersion': torch.__version__,
        'cudaVersion': torch.version.cuda if torch.cuda.is_available() else None,
        'cudaAvailable': torch.cuda.is_available(),
        'gpuName': torch.cuda.get_device_name(0) if torch.cuda.is_available() else None,
        'gpuCompatible': False,
    }
    if torch.cuda.is_available():
        try:
            print("Testing CUDA matrix multiplication...")
            x = torch.randn(10, 10, device='cuda')
            y = torch.randn(10, 10, device='cuda')
            z = torch.mm(x, y)
            torch.cuda.synchronize()
            del x, y, z
            torch.cuda.empty_cache()
            info['gpuCompatible'] = True
            print("GPU is COMPATIBLE!")
        except RuntimeError as e:
            info['gpuCompatible'] = False
            info['error'] = str(e)[:200]
            print(f"GPU is NOT COMPATIBLE: {str(e)[:200]}")
    print(json.dumps(info, indent=2))
except Exception as e:
    print(f"Error: {e}")
