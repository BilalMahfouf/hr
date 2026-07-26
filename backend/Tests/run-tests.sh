#! /usr/bin/bash

dotnet test Application.Tests

echo "========================="
echo "TEST SUMMARY"
echo "$summary"
echo "========================="
