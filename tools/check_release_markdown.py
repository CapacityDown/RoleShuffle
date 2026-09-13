"""Validate release Markdown using Thunderstore's UTF-8 and character limits.

Run before packaging: python tools/check_release_markdown.py
Source: https://github.com/thunderstore-io/Thunderstore/blob/master/django/thunderstore/repository/validation/markdown.py
"""

from pathlib import Path
import argparse
import codecs


MAX_MARKDOWN_CHARACTERS = 100_000


def validate_markdown(path: Path) -> str:
    data = path.read_bytes()
    text = data.decode('utf-8')
    if data.startswith(codecs.BOM_UTF8):
        raise ValueError(f'{path.name} must be UTF-8 without a BOM')
    if len(text) > MAX_MARKDOWN_CHARACTERS:
        raise ValueError(f'{path.name} is too long: {len(text):,} characters; max: {MAX_MARKDOWN_CHARACTERS:,}')
    return text


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--package', type=Path, default=Path(__file__).resolve().parents[1] / 'package')
    args = parser.parse_args()
    for name in ('README.md', 'CHANGELOG.md'):
        try:
            text = validate_markdown(args.package / name)
        except (OSError, UnicodeError, ValueError) as error:
            parser.exit(1, f'{error}\n')
        print(f'{name}: {len(text):,}/{MAX_MARKDOWN_CHARACTERS:,} characters, UTF-8 without BOM')


if __name__ == '__main__':
    main()
