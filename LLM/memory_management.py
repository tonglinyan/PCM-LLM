import torch
import numpy as np
import torch
import copy
from utils import *
import pymongo
import time
from bson import Binary

class History(TypedDict):
    index: int
    time_stamp: str
    context: str
    triples: str
    query: str
    inner_speech: str
    output: str
    summary: Dict
    embedding: List[float]
    image: Binary
    
    
class UpdatingHistory(TypedDict):
    index: int
    time_stamp: str
    context: str
    triples: str
    query: str
    preference_updating: str
    list: List
    
"""
class EmotionValence(TypedDict):
    positive: float
    negative: float
    
class Emotions(TypedDict):
    FacialExpression: EmotionValence 
    PhysiologicalExpression: EmotionValence
        
class Move(TypedDict):
    action: str
    direction: str

class LLMOutput(TypedDict):
    Emotion: Emotions 
    Move: Move 
"""

class MemoryManagement(object):
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
        
        self.history_path = simulationId + f"{model_id}.json"  
        self.preference_path = simulationId + f"{model_id}_updating.json"
        
        history_doc = self.collection.find_one({"user_id": self.history_path})
        if history_doc:
            self.history = history_doc.get("data", {})
        else:
            self.history = {}
        
        pref_doc = self.collection.find_one({"user_id":self.preference_path})
        if pref_doc:
            self.pref_history = pref_doc.get("data", {})
        else:
            self.pref_history = {}
            
        """
        if os.path.exists(self.history_path):    
            with open(self.history_path, "r") as file:
                # Load JSON data from the file
                self.history = json.load(file)
        else:
            folder = os.path.dirname(self.history_path)
            if not os.path.exists(folder):
                os.mkdir(folder)
            self.history = dict() 
        
        if os.path.exists(self.preference_path):    
            with open(self.preference_path, "r") as file:
                # Load JSON data from the file
                self.pref_history = json.load(file)
        else:
            folder = os.path.dirname(self.preference_path)
            if not os.path.exists(folder):
                os.mkdir(folder)
            self.pref_history = dict() 
        """
            

            
    def write_history(self):
        """
        Saving the history with embeddings and without embeddings
        """
        
        """
        with open(self.history_path, "w") as json_file:
            json.dump(self.history, json_file, indent=4)
        """
        
        # Insert the history
        self.collection.replace_one(
            {"user_id": self.history_path},  
            {
                "user_id": self.history_path,
                "data": self.history,
                "last_updated": time.time() 
            },
            upsert=True  
        )
            
        history = copy.deepcopy(self.history)
        for host in history.keys():
            for user in history[host].keys():
                dialogs = history[host][user]
                for dialog in dialogs:
                    del dialog["embedding"]
                    del dialog["image"]
                        
        clean_path = self.history_path.replace('.json', '_clean.json')
        
        # Insert the clean history
        self.collection.replace_one(
            {"user_id": clean_path }, 
            {
                "user_id": clean_path,
                "data": history,
                "last_updated": time.time()
            },
            upsert=True  
        )
        
        
        """
        with open(clean_path, "w") as json_file:
            json.dump(history, json_file, indent=4)
        """
            
            
    def write_pref_history(self):
        """
        Saving the preference updating history with embeddings and without embeddings
        """
        # Insert the preference udpating
        self.collection.replace_one(
            {"user_id": self.preference_path},  
            {
                "user_id": self.preference_path,
                "data": self.pref_history,
                "last_updated": time.time() 
            },
            upsert=True  
        )
        """
        with open(self.preference_path, "w") as json_file:
            json.dump(self.pref_history, json_file, indent=4)
        """

    
    def _similarity(self, v1, v2):
        """
        Calculate the similarity between two conversation embeddings
        """
        vec1 = torch.tensor(v1)
        vec2 = torch.tensor(v2)
        cos_sim = torch.nn.functional.cosine_similarity(vec1, vec2, dim=0)
        return cos_sim.tolist()
    
    
    def get_recent_turn(self, user, host, j=1) :
        """
        According to the query, extract the recent conversation and the most related conversation turns
        Outputs: 
            top-j recent conversation turn, 
        """
        
        if not host in self.history.keys():
            self.index = 1
            self.history[host] = {user: []}
            self.pref_history[host] = {user:[]}
        elif not user in self.history[host].keys():
            self.index = 1
            self.history[host][user] = []
            self.pref_history[host][user] = []
        else:
            self.index = len(self.history[host][user]) + 1
        
        # check if there are last turns of conversation
        dialog = self.history[host][user]

        if len(dialog) < 1:
            conv_rec = None
        elif len(dialog) < j:
            conv_rec = dialog
        else: 
            conv_rec = dialog[-j:]
        
        return conv_rec  
    
        
    def get_related_turn(self, q_embedding, history_calling, user, host, threshold=0.85, k=1, j=1) :
        """
        According to the query, extract the recent conversation and the most related conversation turns
        Outputs: 
            top-j recent conversation turn, 
            top-k related conversation turns
        """
        
        if not host in self.history.keys():
            self.index = 1
            self.history[host] = {user: []}
            self.pref_history[host] = {user:[]}
        elif not user in self.history[host].keys():
            self.index = 1
            self.history[host][user] = []
            self.pref_history[host][user] = []
        else:
            self.index = len(self.history[host][user]) + 1
        
        # check if there are last turns of conversation
        dialog = self.history[host][user]

        if len(dialog) < 1:
            conv_rec = None
        else: 
            conv_rec = dialog[-j:]
            
        # check if there is a conversation history between host and user
        if self.index == 1 or not history_calling:
            conv_rel = None
        else: 
            flat_dialog = [d for xd in dialog for d in xd]
            flat_dialog = flat_dialog[:-1]
            
            # last turn of conversation will be returned directly
            sim_lst = [self._similarity(q_embedding, v["embedding"]) for v in flat_dialog ]

            # convert to numpy array
            arr = np.array(sim_lst)
            print("\nSimilarity score: \n", arr)

            topk_indices = arr.argsort()[-k:]
            indices_above_threshold = [index for index in topk_indices if arr[index] > threshold]
            if len(indices_above_threshold) > 0: 
                conv_rel = [flat_dialog[t] for t in indices_above_threshold]
            else: 
                conv_rel = None
        
        return conv_rec, conv_rel    
    
    
    def insert_preference_updating(self, context, triples, query, output, list, user, host, timeStamp):
        if True:
            dialogs = self.pref_history[host][user]
            dialogs.append(UpdatingHistory(index=self.index, 
                                            time_stamp=timeStamp,
                                            context=context, 
                                            triples=triples, 
                                            query=query, 
                                            preference_updating=output,
                                            list=list))

    
    def insert_new_conversation(self, context, triples, query, inner, expressed, summary, embedding, user, host, timeStamp, image=None):
        """
        Add new conversation turn in history
        """           
        if True:
            
            if image is not None:
                """
                if isinstance(image, str) :
                    image_bytes = base64.b64decode(image)
                else:
                    # convert PIL Image into bytes
                    buffer = io.BytesIO()
                    #image.save(buffer, format="PNG")
                    image_bytes = buffer.getvalue()
                """
                image_bytes = image
            else:
                image_bytes = None
                
            # print("image byte: ", image_bytes)
            
            dialogs = self.history[host][user]
            dialogs.append(History(index=self.index, 
                                    time_stamp=timeStamp,
                                    context=context, 
                                    triples=triples, 
                                    query=query, 
                                    inner_speech=inner,
                                    output=expressed,
                                    summary=summary,    
                                    embedding=embedding, 
                                    image=image_bytes))
        """
        else: 
            users = ["system", "ROSIE"]
            for user in users:
                if user not in self.history[host].keys():
                    self.history[host][user] = [[]]
                    
                dialogs = self.history[host][user]
                current_turn = len(dialogs) - 1
                dialogs[current_turn].append(History(index=self.index, 
                                    triples=None, 
                                    query=None, 
                                    inner_speech=None,
                                    output=expressed,
                                    preference_updating=pref_updating,
                                    summary=summary,    
                                    embedding=embedding))
        """

