import json
import os
from utils import *
import pymongo


def upload_dialogues_to_mongodb(json_folder_path="./Histories", connection_string="mongodb://root:root@172.27.10.131:7001"):
    client = pymongo.MongoClient(connection_string)
    db = client["histories"]
    
    # List all .json files in the folder
    for timestamp in os.listdir(json_folder_path):
        
        if not os.path.isfile(timestamp):
            # Optional: Use a unified collection or generate per file
            folder_path = os.path.join(json_folder_path, timestamp)
            if len(os.listdir(folder_path)) > 0:
            
                collection = db[timestamp]  # change if needed
                for filename in os.listdir(folder_path):
                    if filename.endswith(".json"):

                        file_path = os.path.join(folder_path, filename)

                        # Extract _id from filename: e.g., "05201459_simulation5.json" → "05201459_simulation5"
                        _id = filename  # or generate other logic if needed

                        # Read the JSON content
                        with open(file_path, "r", encoding="utf-8") as f:
                            try:
                                data = json.load(f)
                            except json.JSONDecodeError:
                                print(f"Failed to parse {file_path}")
                                continue

                        # Add _id to the document if not present
                        if "_id" not in data:
                            data["_id"] = _id

                        # Check if the document already exists
                        existing = collection.find_one({"_id": data["_id"]})
                        if existing:
                            print(f"Document {_id} already exists. Skipping insert.")
                            continue

                        # Insert the document
                        collection.insert_one(data)
                        print(f"Inserted document: {_id}")
        else:
            collection = db["others"]  # change if needed
            _id = timestamp  # or generate other logic if needed

            # Read the JSON content
            with open(os.path.join(json_folder_path, timestamp), "r", encoding="utf-8") as f:
                try:
                    data = json.load(f)
                except json.JSONDecodeError:
                    print(f"Failed to parse {os.path.join(json_folder_path, timestamp)}")
                    continue

            # Add _id to the document if not present
            if "_id" not in data:
                data["_id"] = _id

            # Check if the document already exists
            existing = collection.find_one({"_id": data["_id"]})
            if existing:
                print(f"Document {_id} already exists. Skipping insert.")
                continue

            # Insert the document
            collection.insert_one(data)
            print(f"Inserted document: {_id}")

upload_dialogues_to_mongodb() 