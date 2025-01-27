#!/bin/bash

# Get the directory of the current script
SCRIPT_DIR=$(dirname "$(realpath "$0")")

# Path to the configuration JSON file
CONFIG_FILE="$SCRIPT_DIR/launch.json"

# Build each project in Release mode
echo "Building all projects in Release mode..."

if [ ! -f "$CONFIG_FILE" ]; then
  echo "Error: Configuration file '$CONFIG_FILE' not found."
  exit 1
fi

APPS=$(jq -c '.applications[]' "$CONFIG_FILE")

for APP in $APPS; do
    echo "App JSON: $APP"
    APP_PATH=$(echo "$APP" | jq -r '.path')
    echo "Extracted path: $APP_PATH"

    # Extract the project directory
    PROJECT_REL_PATH=$(echo "$APP_PATH" | sed 's|/bin/Release/.*||')
    echo "Project relative path: $PROJECT_REL_PATH"

    PROJECT_PATH="$SCRIPT_DIR/$PROJECT_REL_PATH"
    echo "Project absolute path: $PROJECT_PATH"

    if [ ! -f "$PROJECT_PATH/*.csproj" ]; then
        echo "Error: No .csproj file found in $PROJECT_PATH"
        continue
    fi

    # Build the project
    echo "Building project: $PROJECT_PATH"
    dotnet build "$PROJECT_PATH" --configuration Release
    if [ $? -ne 0 ]; then
        echo "Error: Failed to build $PROJECT_PATH"
        exit 1
    fi
done

echo "Build process completed."

# Launch the built applications
echo "Launching applications..."

for APP in $APPS; do
  # Extract the application path and parameters
  APP_REL_PATH=$(echo "$APP" | jq -r '.path')
  APP_PARAMS=$(echo "$APP" | jq -r '.parameters')

  # Resolve the full path of the application
  APP_PATH="$SCRIPT_DIR/$APP_REL_PATH"

  # Check if the application file exists
  if [ ! -f "$APP_PATH" ]; then
    echo "Error: Application file '$APP_PATH' not found."
    continue
  fi

  # Launch the application
  echo "Launching $APP_PATH with parameters: $APP_PARAMS"
  dotnet "$APP_PATH" $APP_PARAMS &
done

# Wait for all background processes to finish
wait

echo "All applications have been launched."
