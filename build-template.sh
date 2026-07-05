#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_DIR="$SCRIPT_DIR/src/OldSchoolFormsUi.Templates"
OUTPUT_DIR="$PROJECT_DIR/bin/Release"

echo "=== OldSchoolFormsUi.Templates Build ==="

if ! command -v dotnet &> /dev/null; then
  echo "Fehler: dotnet CLI nicht gefunden"
  exit 1
fi

echo "dotnet version: $(dotnet --version)"

echo ""
echo "Schritt 1: Template-Package bauen..."
dotnet pack "$PROJECT_DIR" -c Release --output "$OUTPUT_DIR"

NUPKG=$(ls -1 "$OUTPUT_DIR"/*.nupkg 2>/dev/null | head -1)

if [ -z "$NUPKG" ]; then
  echo "Fehler: Keine .nupkg Datei gefunden"
  exit 1
fi

echo ""
echo "Ergebnis: $NUPKG"
echo "Dateigröße: $(du -h "$NUPKG" | cut -f1)"
echo ""
echo "Zum Veröffentlichen:"
echo "  dotnet nuget push \"$NUPKG\" --source https://api.nuget.org/v3/index.json"
echo ""
echo "Zum Testen (lokal installieren):"
echo "  dotnet new install \"$NUPKG\""
echo "  dotnet new oldschoolforms-app -n MeinProjekt"
echo "  dotnet new uninstall OldSchoolFormsUi.Templates"
