"""Run locally from a terminal. The key is hidden and sent to dotnet via stdin."""
import getpass
import json
from pathlib import Path
import subprocess

project = Path(__file__).resolve().parents[1] / 'CNPM_EnglishLearning.csproj'
key = getpass.getpass('Dán Supabase Secret key hoặc service_role (không hiện ký tự): ').strip()
if not key or not (key.startswith('sb_secret_') or key.startswith('eyJ')):
    raise SystemExit('Cần Secret key hoặc service_role JWT, không dùng publishable/anon key.')
if key.startswith('eyJ'):
    import base64
    try:
        part = key.split('.')[1]
        role = json.loads(base64.urlsafe_b64decode(part + '=' * (-len(part) % 4))).get('role')
    except Exception:
        raise SystemExit('JWT không hợp lệ.')
    if role != 'service_role':
        raise SystemExit('Cần khóa service_role, không dùng anon hoặc token người dùng.')
result = subprocess.run(['dotnet', 'user-secrets', 'set', '--project', str(project)],
                        input=json.dumps({'Supabase:SecretKey': key}), text=True, capture_output=True)
if result.returncode:
    raise SystemExit('Không lưu được User Secrets. Kiểm tra .NET SDK và quyền thư mục User Secrets.')
print('Đã lưu khóa phía server. Chạy: ASPNETCORE_ENVIRONMENT=Development dotnet run. Không commit hoặc gửi khóa qua chat.')
