#!/bin/sh

cd MikuMemories/MikuMemories

# Kill any running instances of the program
program_name="MikuMemories"
old_instances=$(pgrep -f "$program_name")
if [ ! -z "$old_instances" ]; then
    echo "Killing old instances of the program:"
    echo "$old_instances"
    kill -9 $old_instances
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
