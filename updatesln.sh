#!/bin/bash

rm *.sln
rm *.slnx

find "./source" -type f -name "*.sln" -exec rm -v {} \;
find "./source" -type f -name "*.slnx" -exec rm -v {} \;

./Contrib/pjr2sln.sh ./JobManager.Triggers.slnx -Path=./Source

rm *.sln
