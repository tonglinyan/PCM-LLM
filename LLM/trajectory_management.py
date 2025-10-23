from utils import *
import pymongo
import time
import base64
import io
from bson import Binary
    
class ActionHistory(TypedDict):
    time_stamp: str
    triples: str
    full_output: str
    output: str
    image: Binary
    

class ActionManagement(object):
    """
    history format
    {
        "agent 0": 
        {
            "agent 1": [
                    [history1, history2, history3, ...],
                    [history1, history2, history3, ...]
                }
            ]
            "agent 2": [...],
        }, 
        "agent 1":{
            "agent 0": [...], 
            "agent 2": [...]
        }
    }
    """
    def __init__(self, history_path, model_id, connection_string="mongodb://root:root@172.27.10.131:7001"):
        self.load_history(history_path, model_id, connection_string)
        #self.new_conversation_turn()
        
    
    def load_history(self, filepath, model_id, connection_string) -> None:
        # Connect to MongoDB
        self.client = pymongo.MongoClient(connection_string)
        self.db = self.client["histories"]
        
        timestamp = "others"
        simulationId = filepath

        if "\\" in filepath:
            timestamp, simulationId = filepath.split("\\")
            
        self.collection = self.db[timestamp]
        self.history_path = simulationId + f"{model_id}_actions.json"  
        
        history_doc = self.collection.find_one({"user_id": self.history_path})
        if history_doc:
            self.history = history_doc.get("data", {})
        else:
            self.history = {}
            
            
    def write_history(self):
        """
        Saving the preference updating history with embeddings and without embeddings
        """
        # Insert the preference udpating
        self.collection.replace_one(
            {"user_id": self.history_path},  
            {
                "user_id": self.history_path,
                "data": self.history,
                "last_updated": time.time()
            },
            upsert=True  
        )
            
    
    def insert_new_action(self, triples, full_output, output, host, timeStamp, image=None):
        if True:
            if host not in self.history:
                self.history[host] = []
                
            if image is not None:
                if isinstance(image, str) and image.startswith("data:image"):
                    _, image_data = image.split(",", 1)
                    image_bytes = base64.b64decode(image_data)
                else:
                    buffer = io.BytesIO()
                    image.save(buffer, format="PNG")
                    image_bytes = buffer.getvalue()
            else:
                image_bytes = image
                
            dialogs = self.history[host]
            dialogs.append(
                ActionHistory(
                    time_stamp=timeStamp,
                    triples=triples, 
                    full_output=full_output,
                    output=output,
                    image=image_bytes
                )
            )