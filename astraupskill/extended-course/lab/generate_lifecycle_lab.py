"""Generate an isolated current-source lifecycle characterization lab."""
import argparse
import hashlib
import json
from pathlib import Path


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output', required=True)
    parser.add_argument('--mode', choices=['reference', 'starter'], default='reference')
    args = parser.parse_args()
    here = Path(__file__).resolve().parent
    root = here.parents[2]
    output = Path(args.output).expanduser()
    if not output.is_absolute():
        parser.error('Output must be a new absolute external directory.')
    output = output.resolve()
    if output.exists() or output == root or root in output.parents:
        parser.error('Existing and in-project outputs are refused.')
    names = ['Common/Entity.cs', 'Common/DomainException.cs', 'Enums.cs',
             'FileNode.cs', 'FileVersion.cs', 'ProcessingJob.cs', 'ShareLink.cs']
    prepared = []
    for name in names:
        relative = 'src/NetDropbox.Domain/' + name
        raw = (root / relative).read_bytes()
        copied = raw
        mutation = None
        if args.mode == 'starter' and name == 'ShareLink.cs':
            source = raw.decode('utf-8-sig')
            anchor = 'ExpiresAt > now'
            if source.count(anchor) != 1:
                raise ValueError('Expected expiry expression changed; review starter.')
            copied = source.replace(anchor, 'ExpiresAt >= now').encode('utf-8')
            mutation = 'Deliberately accept the exact expiry instant.'
        prepared.append((relative, raw, copied, mutation))
    program = (here / 'LifecycleProgram.cs.txt').read_text(encoding='utf-8')
    output.mkdir(parents=True, exist_ok=False)
    manifest = []
    for relative, raw, copied, mutation in prepared:
        target = output / relative
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_bytes(copied)
        manifest.append({'path': relative, 'source_sha256': hashlib.sha256(raw).hexdigest(),
                         'copied_sha256': hashlib.sha256(copied).hexdigest(), 'mutation': mutation})
    (output / 'Program.cs').write_text(program, encoding='utf-8')
    (output / 'Lab.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable></PropertyGroup></Project>\n', encoding='utf-8')
    (output / 'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>\n', encoding='utf-8')
    (output / 'SOURCE-MAP.json').write_text(json.dumps({'mode': args.mode, 'files': manifest}, indent=2)+'\n', encoding='utf-8')
    print(output)


if __name__ == '__main__':
    main()
