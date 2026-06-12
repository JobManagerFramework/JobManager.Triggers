#!/bin/bash

# Funktion zur Anzeige der Hilfe
show_help() {
  echo "Verwendung: $0 <PfadZurSlnOderSlnxDatei> [-Path=Suchverzeichnis]"
  exit 1
}

# Überprüfe, ob mindestens ein Argument übergeben wurde
if [ $# -lt 1 ]; then
  show_help
fi

# Erstes Argument ist der Pfad zur SLN- oder SLNX-Datei
solution_file="$1"
shift

# In absoluten Pfad umwandeln
solution_file=$(realpath "$solution_file")

# Dateiendung extrahieren
extension="${solution_file##*.}"

# Unterstützte Endungen prüfen
if [[ "$extension" != "sln" && "$extension" != "slnx" ]]; then
  echo "Fehler: Nur .sln und .slnx Dateien werden unterstützt."
  exit 1
fi

# Standard: Verzeichnis der SLN-Datei
search_path=$(dirname "$solution_file")

# Verarbeite optionale Parameter
for arg in "$@"; do
  case $arg in
    -Path=*)
      search_path="${arg#*=}"
      shift
      ;;
    *)
      echo "Unbekannter Parameter: $arg"
      show_help
      ;;
  esac
done

# Basisname ohne Endung
base_name="$(basename "$solution_file" .$extension)"
base_dir="$(dirname "$solution_file")"
sln_file="$base_dir/$base_name.sln"

# Erstelle die SLN-Datei, falls sie nicht existiert
if [ ! -f "$sln_file" ]; then
  echo "SLN-Datei existiert nicht. Erstelle neue SLN-Datei..."
  dotnet new sln -n "$base_name" -o "$base_dir"
fi

# Konvertiere zu SLNX, falls gewünscht
if [[ "$extension" == "slnx" ]]; then
  echo "Konvertiere SLN-Datei zu SLNX..."
  dotnet sln "$sln_file" migrate
fi

# Füge alle .csproj-Dateien zur SLN/SLNX-Datei hinzu
find "$search_path" -name "*.csproj" -exec dotnet sln "$solution_file" add {} \;

echo "Alle .csproj-Dateien im Verzeichnis '$search_path' wurden zur Solution-Datei '$solution_file' hinzugefügt."
