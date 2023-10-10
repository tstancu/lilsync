#!/bin/bash

#no of arguments is not equal to 3?

if [ $# -ne 3 ]; then
    echo "Usage: $0 <source_directory> <replica_directory> <log_directory> "
    exit 1
fi

script_dir="$( cd "$( dirname "$BASH_SOURCE[0]}" )" && pwd )"

source_directory="$1"
replica_directory="$2"
log_directory="$3"

bin_folder="../bin"
obj_folder="../obj"

# replica folder exists

if [ -d "$replica_directory" ]; then
    rm -rf "$replica_directory"/*
    echo "Contents of $replica_directory have been deleted."

    # remove log directory
    
    if [ -d "$log_directory" ]; then
        echo "Deleting contents of $log_directory..."
        rm -rf "$log_directory"
        echo "Log directory and its contents have been deleted."
    else
        echo "$log_directory does not exist."
    fi

    # remove obj and bin

    if [ -d "/${script_dir}/${obj_folder}" ]; then
        rm -rf "/${script_dir}/${obj_folder}"
        echo "obj directory has been deleted from $source_directory."
    fi

    if [ -d "${script_dir}/${bin_folder}" ]; then
        rm -rf "${script_dir}/${bin_folder}"
        echo "bin directory has been deleted from $source_directory."
    fi
else
    echo "$replica_directory does not exist."
fi

