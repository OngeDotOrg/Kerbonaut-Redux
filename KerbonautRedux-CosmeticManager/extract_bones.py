#!/usr/bin/env python3
import re


bones = set()
with open('/dev/stdin') as f:
    for line in f:
        match = re.search(r'- (bn_[a-zA-Z0-9_]+)', line)
        if match:
            bones.add(match.group(1))


for bone in sorted(bones):
    print(f'    "{bone}",')
