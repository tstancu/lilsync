#!/bin/bash

#no of arguments is not equal to 2?

if [ $# -ne 3 ]; then
    echo "Usage: $0 <source_directory> <replica_directory> <log_directory>"
    exit 1
fi

source_directory="$1"
replica_directory="$2"
log_directory="$3"

# replica folder exists

if [ -d "$replica_directory" ]; then
    rm -rf "$replica_directory"/*
    echo "Contents of $replica_directory have been deleted."

    if [ -d "$log_directory" ]; then
        echo "Deleting contents of $log_directory..."
        rm -rf "$log_directory"/*
        echo "Log directory and its contents have been deleted."
    else
        echo "$log_directory does not exist."
    fi
else
    echo "$replica_directory does not exist."
fi

