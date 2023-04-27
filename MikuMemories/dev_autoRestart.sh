#!/bin/bash

# Set the path to the file to be monitored
file_path="/home/io/MikuMemories/MikuMemories/bin/Debug/net7.0/MikuMemories.dll"

# Use fuser to find the PID of the process accessing the file
pid=$(fuser -k "$file_path")

# If a PID was found, terminate the process
if [ -n "$pid" ]; then
  echo "Process $pid accessing $file_path found and terminated."
else
  echo "No processes accessing $file_path found."
fi



# Function to clean up and exit
clean_exit() {
    if [ ! -z "$terminator_pid" ]; then
        kill -9 $terminator_pid
    fi
    exit 1
}

# Trap SIGTSTP (Ctrl+Z), SIGHUP (terminal closed), and call clean_exit function
trap clean_exit SIGTSTP SIGHUP

# Build the new instance
dotnet build

# Run the new instance in a new terminator terminal
terminator -e "dotnet run --no-build main_c-i_tavern.png; read" & terminator_pid=$!

wait $terminator_pid
