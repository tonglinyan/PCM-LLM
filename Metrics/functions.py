"""
Analysis functions for PCM-LLM simulation data.

This module provides utilities for:
- Extracting and processing PCM trajectory data from MongoDB
- Evaluating LLM conversational outputs (BLEU, ROUGE, BERTScore)
- Visualizing agent preferences, emotions, and spatial dynamics
- Statistical analysis of experimental results
"""

import os
import pandas as pd
import numpy as np
import json
import pymongo
import pprint
pp = pprint.PrettyPrinter()
from fractions import Fraction
from collections import defaultdict
from scipy.stats import chi2_contingency
from nltk.translate.bleu_score import sentence_bleu
from rouge import Rouge
from bert_score import score
from sentence_transformers import SentenceTransformer, util
from ipywidgets import widgets
from IPython.display import display
import re
from tqdm import tqdm

# Entity mapping for treasure-hunting game
entityDict = {
    0: "Subject",          # Virtual agent (Marie)
    1: "Participant",      # Human player
    2: "Dark brown box",   # Object 1
    3: "Light brown box"   # Object 2
}
n_agent = 2
n_entity = len(entityDict)


def dfs(node, best_path, best_value):
    """
    Depth-first search to extract path marked as 'best' in prediction tree.

    Args:
        node: Current node (dict with 'bestNode', 'score', 'children')
        best_path: Accumulator for best path nodes
        best_value: Accumulator for scores along path

    Returns:
        tuple: (best_path, best_value) - lists of nodes and scores
    """
    if node["bestNode"] == True:
        best_path.append(node)
        best_value.append(node["score"])
        for child in node["children"]:
            dfs(child, best_path, best_value)
    return best_path, best_value


def find_best_path(root):
    """
    Extract the path marked as 'best' through a prediction tree.

    This function follows nodes where bestNode=True, extracting the
    sequence of predicted states chosen by the PCM planner.

    Args:
        root: Root node of prediction tree

    Returns:
        tuple: (best_path, best_value)
            - best_path: List of nodes along best path
            - best_value: List of scores at each node
    """
    if not root:
        return [], float('inf')

    best_path = []
    best_value = []

    best_path, best_value = dfs(root, best_path, best_value)

    return best_path, best_value

    
def extract_information(data):
    """
    Extract time series data from a sequence of agent states.

    Parses nested state dictionaries and organizes preferences, emotions,
    spatial stats, and positions into structured time series.

    Args:
        data: List of state dictionaries (from PCM predictions)

    Returns:
        tuple: (extracted_data, position, text, player_left_target, player_right_target)
            - extracted_data: Dict with time series for all variables
            - position: Dict with X/Z coordinates and look-at directions
            - text, player_*_target: Additional interaction data
    """
    agentId = data[0]["CurrentAgentId"]

    target = []
    text = {}
    player_left_target = []
    player_right_target = []

    # Initialize nested dictionaries for time series
    pref = { entityDict[i]: { key: [] for key in entityDict.values()} for i in range(n_agent) }
    postpref = { entityDict[i]: { key: [] for key in entityDict.values()} for i in range(n_agent) }
    affective = { entityDict[i]: { key: [] for key in entityDict.values()} for i in range(n_agent) }
    epistemic = { entityDict[i]: { key: [] for key in entityDict.values()} for i in range(n_agent) }
    fe = { entityDict[i]: { key: [] for key in entityDict.values()} for i in range(n_agent) }
    spatialstat = { entityDict[i]: { key: [] for key in entityDict.values()} for i in range(n_agent) }
    
    tom = {"agent":[], "participant":[]}
    val = { entityDict[i]: {"Felt": [], "Facial": [], "Physiological": [], "Voluntary Facial": []} for i in range(n_agent) }
    position = {entityDict[i]: {"X": [], "Z": [], "vertices_X": [], "vertices_Z": [], "LA_X": [], "LA_Z": []} for i in range(n_entity)}
    
    for value in data:

        for i in range(n_agent):
            agentId = entityDict[i]
            for j in range(n_entity):
                entityId = entityDict[j]
                
                # get preference
                pref[agentId][entityId].append(value["Preference"][i][j])
                postpref[agentId][entityId].append(value["PostPreference"][i][j])
                epistemic[agentId][entityId].append(value["Certainty"][i][j])
                affective[agentId][entityId].append(value["Mu"][i][j])
                fe[agentId][entityId].append(value["Free Energy"][i][j])
                spatialstat[agentId][entityId].append(value["SpatialStat"][i][j])
            
            # get valence
            val[agentId]["Felt"].append(value["Emotions"][i]["Felt"]["Val"])
            val[agentId]["Facial"].append(value["Emotions"][i]["Facial"]["Val"])
            val[agentId]["Physiological"].append(value["Emotions"][i]["Physiological"]["Val"])
            val[agentId]["Voluntary Facial"].append(value["Emotions"][i]["VoluntaryFacial"]["Val"])
            
        for i in range(n_entity):
            position[entityDict[i]]["X"].append(-value["Positioning"][i]["BodyPosition"]["Center"]["X"])
            position[entityDict[i]]["Z"].append(value["Positioning"][i]["BodyPosition"]["Center"]["Z"])
            position[entityDict[i]]["LA_X"].append(-value["Positioning"][i]["LookAt"]["X"])
            position[entityDict[i]]["LA_Z"].append(value["Positioning"][i]["LookAt"]["Z"])
            vertices = value["Positioning"][i]["BodyPosition"]["Vertices"]
            position[entityDict[i]]["vertices_X"].append([-vertices[j]["X"] for j in [0, 1, 5, 4, 0]])
            position[entityDict[i]]["vertices_Z"].append([vertices[j]["Z"] for j in [0, 1, 5, 4, 0]])

    extracted_data = {
        "valence": val,  
        "certainty": epistemic, 
        "preference": pref, 
        "postpreference": postpref,
        "spatial stat": spatialstat,
        "affective value": affective,
        "free energy": fe,
    }

    return extracted_data, position, text, player_left_target, player_right_target


def preprocessing(documents):
    """
    Process prediction tree documents into organized trajectory data.

    Extracts real states and predicted states (t+1, t+2) from MongoDB
    trajectory documents, separating agent and participant perspectives.

    Args:
        documents: List of prediction tree documents from MongoDB

    Returns:
        dict: Dictionary with keys:
            - real_agent_data, pred1_agent_data, pred2_agent_data
            - real_part_data, pred1_part_data, pred2_part_data
            - real_position, pred1_position_agent, pred2_position_agent
            - pred1_position_part, pred2_position_part
    """
    real_agent = []
    pred1_agent = []
    pred2_agent = []
    real_part = []
    pred1_part = []
    pred2_part = []

    iter = False
    
    for document in enumerate(documents):
        best_path, best_value = find_best_path(document[1])
        #print(document["_id"])
        #print(best_path[1])
        #iter = int(document["_id"].split("_")[1])
        
        if len(best_path) > 2:
            iter = True
            # Handle case when best_path has more than 2 elements
            if best_path[0]["state"]["CurrentAgentId"] == 0:    
                real_agent.append(best_path[0]["state"])
                pred1_agent.append(best_path[1]["state"])
                pred2_agent.append(best_path[2]["state"])
            else:
                real_part.append(best_path[0]["state"])
                pred1_part.append(best_path[1]["state"])
                pred2_part.append(best_path[2]["state"])
        else:
            # Handle case when best_path has exactly 2 elements
            if best_path[0]["state"]["CurrentAgentId"] == 0:   
                real_agent.append(best_path[0]["state"])
                pred1_agent.append(best_path[1]["state"])
            else:
                real_part.append(best_path[0]["state"])
                pred1_part.append(best_path[1]["state"])

    real_agent = [real_agent[0]] + real_agent
    real_part = [real_part[0]] + real_part
    pred1_agent = [real_agent[0], real_agent[0]] + pred1_agent
    pred1_part = [real_part[0], real_agent[0]] + pred1_part
    if iter:
        pred2_agent = [real_agent[0], real_agent[0], real_agent[0]] + pred2_agent
        pred2_part = [real_part[0], real_part[0], real_agent[0]] + pred2_part
    
    dictionary = {
        "real_agent_data": None,
        "pred1_agent_data": None,
        "pred2_agent_data": None,
        "real_position": None,
        "pred1_position_agent": None,
        "pred2_position_agent": None,
        "real_part_data": None,
        "pred1_part_data": None,
        "pred2_part_data": None,
        "pred1_position_part": None,
        "pred2_position_part": None,
    }
    
    dictionary["real_agent_data"], dictionary["real_position"], _, _, _ = extract_information(real_agent[:11])
    dictionary["pred1_agent_data"], dictionary["pred1_position_agent"], _, _, _ = extract_information(pred1_agent[:11])
    dictionary["real_part_data"], _, _, _, _ = extract_information(real_part[:11])
    dictionary["pred1_part_data"], dictionary["pred1_position_part"], _, _, _ = extract_information(pred1_part[:11])
    
    if iter:
        dictionary["pred2_agent_data"], dictionary["pred2_position_agent"], _, _, _ = extract_information(pred2_agent[:11])
        dictionary["pred2_part_data"], dictionary["pred2_position_part"], _, _, _ = extract_information(pred2_part[:11])
    
    return dictionary


def extract_simulation_data(all_simulations, evaluation_result, output_dir="Simulation_results"):
    """
    Extract all agent state trajectories and save to CSV files.

    Creates one CSV file per variable (e.g., preference, emotion) combining
    time series data with experimental metadata.

    Args:
        all_simulations: List of simulation dictionaries from preprocessing()
        evaluation_result: DataFrame with simulation metadata
        output_dir: Directory to save CSV files (default: "Simulation_results")

    Outputs:
        CSV files named: {category}_{subcategory}_{agent}_{variable}.csv
        Each contains metadata columns + time step columns [0, 1, 2, ...]
    """
    series_list = ["real_agent_data", "real_part_data", "pred1_agent_data", "pred1_part_data"]
    
    if not isinstance(evaluation_result, pd.DataFrame):
        evaluation_result = pd.DataFrame(evaluation_result)
    
    if 'Simulation' not in evaluation_result.columns:
        evaluation_result['Simulation'] = range(len(evaluation_result))
    
    variable_data = defaultdict(list)
    
    for sim_idx, sim_data in enumerate(all_simulations):
        for cat, sim in sim_data.items():
            if cat in series_list:
                for category, agents in sim.items(): # Affective value, FE, etc.
                    for agent_type, entities in agents.items(): # Agent, Participant
                        for var_name, time_series in entities.items(): # Felt, Facial, etc.
                            if agent_type.lower() != var_name.lower():
                                # Create unique key for this variable
                                key = f"{cat}_{category}_{agent_type}_{var_name}".lower()
                                variable_data[key].append(time_series)

    # Create DataFrame for each variable and merge with evaluation results
    for var_key, time_series_list in variable_data.items():
        ts_df = pd.DataFrame(time_series_list)
        ts_df.columns = [f"{i}" for i in range(ts_df.shape[1])]
        ts_df['Simulation'] = ts_df.index
        
        merged_df = pd.merge(evaluation_result, ts_df, 
                            left_on='Simulation', 
                            right_on='Simulation',
                            how='left')
        
        safe_filename = f"{var_key.replace(' ', '_').replace('.', '')}.csv"
        file_path = os.path.join(output_dir, safe_filename)
        
        merged_df.to_csv(file_path, index=False)
        

def extract_simulation_data1(all_simulations, evaluation_result, output_dir="Simulation_results"):
    """
    Extract spatial position trajectories and save to CSV files.

    Similar to extract_simulation_data() but specifically for X/Z coordinates.

    Args:
        all_simulations: List of simulation dictionaries from preprocessing()
        evaluation_result: DataFrame with simulation metadata
        output_dir: Directory to save CSV files (default: "Simulation_results")

    Outputs:
        CSV files for position data: {category}_position_{agent}_{x|z}.csv
    """
    series_list = ["real_position", "pred1_position_agent", "pred1_position_part"]
    
    if not isinstance(evaluation_result, pd.DataFrame):
        evaluation_result = pd.DataFrame(evaluation_result)
    
    if 'Simulation' not in evaluation_result.columns:
        evaluation_result['Simulation'] = range(len(evaluation_result))
    
    variable_data = defaultdict(list)
    
    for sim_idx, sim_data in enumerate(all_simulations):
        for cat, sim in sim_data.items():
            if cat in series_list:
                for agent_type, entities in sim.items(): 
                    for var_name, time_series in entities.items(): 
                        if var_name.lower() in ["x", "z"]:
                            key = f"{cat}_position_{agent_type}_{var_name}".lower()
                            variable_data[key].append(time_series)
    
    for var_key, time_series_list in variable_data.items():
        ts_df = pd.DataFrame(time_series_list)
        ts_df.columns = [f"{i}" for i in range(ts_df.shape[1])]
        ts_df['Simulation'] = ts_df.index
        
        merged_df = pd.merge(evaluation_result, ts_df, 
                            left_on='Simulation', 
                            right_on='Simulation',
                            how='left')
        
        safe_filename = f"{var_key.replace(' ', '_').replace('.', '')}.csv"
        file_path = os.path.join(output_dir, safe_filename)
        
        merged_df.to_csv(file_path, index=False)


def answer_processing(answer):
    mapping_dict = {"A": 0, "B": 1, "C": -1}

    if '.' in answer:
        answer = answer.split(".")[0]

    return mapping_dict[answer.strip().upper()]


def get_llm_cognitive_response(df, model, output_dir="../RawData/Simulation_results/"):
    """
    Download LLM conversation histories and extract cognitive responses.

    Retrieves conversation logs from MongoDB, extracts final answers to
    cognitive questions, and saves to local JSON files.

    Args:
        df: DataFrame with columns ['TimeStamp', 'SimulationID']
        model: Model suffix (e.g., "_llama3.1", "_qwen2.5")
        output_dir: Directory to save results CSV

    Returns:
        tuple: (documents, df_updated)
            - documents: List of conversation documents
            - df_updated: DataFrame with added 'AgentSelect' and 'ParticipantSelect' columns
    """

    if not os.path.exists(output_dir):
        os.makedirs(output_dir)

    connection_string = "mongodb://root:root@172.27.10.131:7001"
    client = pymongo.MongoClient(connection_string)
    db = client["histories"]

    documents = []
    agent_answers = []
    part_answers = []
    for (ts, sid) in zip(df["TimeStamp"], df["SimulationID"]):
        collection = db[ts]
        doc = collection.find_one({"user_id": f"simulation{sid}{model}_clean.json"})

        history_path = f"../RawData/{ts}"
        if not os.path.exists(history_path):
            os.mkdir(history_path)

        with open(os.path.join(history_path, doc['user_id']), "w") as f:
            json.dump(doc, f, default=str, indent=4)
        doc["user_id"] = ts+doc["user_id"]
        del doc["_id"]
        documents.append({"TimeStamp": ts, "SimulationID": sid, "Data": doc["data"]})

        part_answers.append(answer_processing(doc["data"]["Marie"]["Participant"][3]["output"]))
        agent_answers.append(answer_processing(doc["data"]["Participant"]["Marie"][2]["output"]))

    df["AgentSelect"] = agent_answers
    df["ParticipantSelect"] = part_answers

    safe_filename = "simulation_result_llm.csv"
    file_path = os.path.join(output_dir, safe_filename)

    df.to_csv(file_path, index=False)

    return documents, df


def find_long_inner_speech(documents):
    """
    Find documents with long inner speech (more than 60 words).
    Returns lists of timestamps and simulation IDs.
    """
    ts_list = []
    sid_list = []

    for doc in documents:
        data = doc["Data"]
        found = False 

        for host in data:
            for speaker in data[host]:
                for conv in data[host][speaker]:
                    output = conv["output"].strip()
                    word_list = output.split()
                    if len(word_list) > 60 :
                        ts_list.append(str(doc["TimeStamp"]))
                        sid_list.append(str(doc["SimulationID"]))
                        found = True  
                        break
                if found:
                    break  
            if found:
                break 
    
    return ts_list, sid_list  


def download_actions_from_database(df, model):
    
    connection_string = "mongodb://root:root@172.27.10.131:7001"
    client = pymongo.MongoClient(connection_string)
    db = client["histories"]

    df = df[df["VerbalMode"] == "Verbal"]
    #subset = df[(df["VirtualAgentRole"]==1) & (df["ToM"]==1) & (df["FacialExpression"]==1) & (df["ParticipantPhysiologicalSensitivity"]== 1) & (df["AgentSuccess"]==1)]

    for (ts, sid) in zip(df["TimeStamp"], df["SimulationID"]):
        #print(ts, sid)
        collection = db[ts]

        doc = collection.find_one({"user_id": f"simulation{sid}{model}_actions.json"})

        history_path = f"../RawData/{ts}"
        if not os.path.exists(history_path):
            os.mkdir(history_path) 

        with open(os.path.join(history_path, doc['user_id']), "w") as f:
            json.dump(doc, f, default=str, indent=4)
        doc["user_id"] = ts + doc["user_id"]
        del doc["_id"]
      
        
def download_trajectories_from_database(df, output_dir):
    """
    Download PCM prediction trajectories from MongoDB.

    Retrieves full prediction tree sequences (21 iterations) for each
    simulation and processes them into structured trajectory data.

    Args:
        df: DataFrame with columns ['TimeStamp', 'SimulationID']
        output_dir: Directory to save JSON trajectory files

    Returns:
        list: List of simulation dictionaries from preprocessing()
            Each contains real/predicted states for agents and participants
    """
    connection_string = "mongodb://root:root@172.27.10.131:7001"
    client = pymongo.MongoClient(connection_string)
    db = client["trajectories"]

    all_simulations = []

    for (ts, sid) in tqdm(zip(df["TimeStamp"], df["SimulationID"])):
        documents = []
        agentid = 0

        for iter in range(21):
            collection_name = f"{ts}\simulation{sid}_{iter}_{agentid}"
            
            collection = db[collection_name]
            doc = collection.find_one({"_id": f"simulation{sid}_{iter}_{agentid}"})

            history_path = f"{output_dir}/{collection_name}.json"
            history_path = history_path.replace("\\", "/")

            folder = os.path.dirname(history_path)
            if not os.path.exists(folder):
                os.mkdir(folder) 

            documents.append(doc)
            with open(history_path, "w") as f:
                json.dump(doc, f, default=str, indent=4)
            agentid = 1 - agentid
        

        dictionary = preprocessing(documents)
        all_simulations.append(dictionary)
        
    return all_simulations
    
