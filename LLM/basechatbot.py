import torch
import time
from utils import *
from memory_management import *
from trajectory_management import *
import re
import json


class BaseChatBot:
    def __init__(self, model_name: str, model_size: str, summerization: bool, history_saving: bool, history_calling: bool, multimodal: bool, language: str = "fr"):
        self.model_name = model_name
        self.summerization = summerization
        self.history_saving = history_saving
        self.history_calling = history_calling
        self.language = language
        self.multimodal = multimodal
        self.device = "cuda" if torch.cuda.is_available() else "cpu"
        self.conv_rec = None
        self.load_models(model_size)

    def load_models(self, model_size: str):
        raise NotImplementedError("Subclasses must implement this method.")

    def create_logs(self, filepath):
        #full_path = "./Histories/" + filepath
        self.filepath = filepath
        self.memory_DB = MemoryManagement(self.filepath, self.model_name)
        self.action_DB = None
        

    def inference(self, input_pcm, max_gen_len=512):
        raise NotImplementedError("Subclasses must implement this method.")

    def truncate_text_to_50_words(self, text):
        sentences = re.split(r"(?<=[.!?]) +", text)
        truncated_text = []
        word_count = 0

        for sentence in sentences:
            sentence_word_count = len(sentence.split())
            if word_count + sentence_word_count > 50:
                break
            truncated_text.append(sentence)
            word_count += sentence_word_count

        return " ".join(truncated_text)


    def prompt_preprocessing(self, input_pcm):
        """
        input prompt under the format: 
            speaker # user # instruction prompt # triples # query (% preference updating prompt)
            
        if preference updating prompt in input prompt:
            we want update the preference, and respond to the query according the context of PCM
        if preference updating prompt not in input prompt:
            we want llm answer the query without any context of PCM, and don't update preference.
        """
        abrv_task = input_pcm.split('#')[0].strip()
        if abrv_task == "PU":
            self.task = "PU"
            self.image = None
            if "image: " in input_pcm:
                input_pcm, image = input_pcm.split("image: ")
                self.image = image
            
            print("------------------ Text preprocessing -----------------")
            
            _, self.user, self.host, self.instruction, self.triples, self.queries = input_pcm.split("#")
            self.instruction = self.instruction.strip()
            self.triples = self.triples.strip() 
            self.triples = self.pref_pos_extraction()           
            self.queries = self.queries.strip()  
            
            input_prompt = self.prompt_formalizing()

        elif abrv_task == "AP":
            self.task = "AP"
            self.image = None
            if "image: " in input_pcm:
                input_pcm, image = input_pcm.split("image: ")
                self.image = image
            
            print("------------------ Text preprocessing -----------------")
            print(input_pcm)
            
            _, self.user, self.host, self.instruction, self.triples, self.queries = input_pcm.split("#")
            self.instruction = self.instruction.strip()
            self.triples = self.triples.strip()       
            self.queries = self.queries.strip()   

            input_prompt = self.prompt_formalizing()
            
        elif abrv_task == "QA":
            self.task = "QA"
            self.image = None
            if "image: " in input_pcm:
                input_pcm, image = input_pcm.split("image: ")
                self.image = image
            
            print("------------------ Text preprocessing -----------------")
            print(input_pcm)

            _, self.user, self.host, self.instruction, self.triples, self.queries = input_pcm.split("#")
            self.instruction = self.instruction.strip()
            self.triples = self.triples.strip()            
            self.queries = self.queries.strip()
                
            q_embedding = self.get_text_embedding(self.queries)
            conv_rec, conv_rel = self.memory_DB.get_related_turn(q_embedding, self.history_calling, self.user, self.host)
            
            input_prompt = self.prompt_formalizing(conv_rec, conv_rel)
            
            ## Printing of preprocessed input prompts:
            print("\nQuestion answering instruction: \n", self.instruction)
            if self.triples is not None:
                print("\nPCM parameters: \n", self.triples)
            if self.queries is not None:
                print("\nQuery: \n", self.queries)
                
            if conv_rec is not None:
                for conv in conv_rec:
                    print("\nRecent conversation: \n", conv["query"], conv["output"])
            if conv_rel is not None:
                for conv in conv_rel:
                    print("\nRelated conversation: \n", conv["query"], conv["output"])
        elif abrv_task == "LQ":
            self.task = "LQ"
            self.image = None
            if "image: " in input_pcm:
                input_pcm, image = input_pcm.split("image: ")
                self.image = image
            
            print("------------------ Text preprocessing -----------------")
            print(input_pcm)

            _, self.user, self.host, self.instruction, self.triples, self.queries = input_pcm.split("#")
            self.instruction = self.instruction.strip()
            self.triples = self.triples.strip()            
            self.queries = self.queries.strip()
            
            if self.conv_rec == None:
                self.conv_rec = self.memory_DB.get_recent_turn(self.user, self.host, j=3)
            input_prompt = self.prompt_formalizing(conv_rec=self.conv_rec)
            
            ## Printing of preprocessed input prompts:
            if self.triples is not None:
                print("\nPCM parameters: \n", self.triples)
            if self.queries is not None:
                print("\nQuery: \n", self.queries)
                
            if self.conv_rec is not None:
                print("\nRecent conversation: \n")
                for conv in self.conv_rec:
                    print(conv["query"], conv["output"])
        
        else:
            print("task is not identified.")
        print(self.task)
        return input_prompt
    
    
    def paranthese_adding(self, triple):
        """
        Add paranthese to triples
        """
        return '{' + triple + '}'
    
    
    def get_text_embedding(self, text):
        """
        Return the vector representation of input text
        """

        input_token = self.t5tokenizer(
            text, 
            return_tensors="pt", 
            max_length=512,
            truncation=True)
        
        vector = self.summerize_model(
            input_ids=input_token.input_ids,
            decoder_input_ids=input_token.input_ids
        )
        
        embedding = vector.encoder_last_hidden_state.mean(dim=1).squeeze().tolist()
        return embedding       

    def pref_pos_extraction(self):
        """
        Extract preference, coordinates from context (all triples)
        """
        print(self.triples.split("'belief at t step': {"))
        current_belief = self.triples.split("'belief at t step': {")[1].split("}")[0]
        triples = current_belief.split(", ")
        selected_triples = []
        for tri in triples: 
            if "preference towards" in tri or "Position" in tri or "Orientation" in tri:
                selected_triples.append(tri)
        return ", ".join(selected_triples)
    
    
    def clean_text(self, text, keep_punctuation=False):

        if keep_punctuation:
            cleaned = re.sub(r"[^\w\s.,!?'\-:]", "", text)
        else:
            cleaned = re.sub(r"[^a-zA-Z0-9\s]", "", text)
        cleaned = re.sub(r"\s+", " ", cleaned).strip()
        cleaned = cleaned.lower()
        return cleaned
    
    
    def qa_postprocessing(self, prompt):
        print(prompt)
        lines = prompt.split('\n')
        inner_speech_lines = []
        output_lines = []
        current_section = None

        for line in lines:
            line = self.clean_text(line, True)
            if "inner speech:" in line or "reasoning:" in line:
                current_section = "inner"
                line = line.replace("inner speech:", "").replace("reasoning:", "")
                print(line)
            elif "output:" in line or "query:" in line:
                current_section = "output"
                line = line.replace("output:", "").replace("query:", "")
                print(line)
            
            if current_section == "inner" and line:
                inner_speech_lines.append(line)
            elif current_section == "output" and line:
                output_lines.append(line)

        inner_speech = " ".join(inner_speech_lines)
        output = " ".join(output_lines)
        
        inner_speech = inner_speech.strip().capitalize()
        output = output.strip().capitalize()
        return inner_speech, output
        
    
    def lq_postprocessing(self, prompt):
        
        print(prompt)
        lines = prompt.split('\n')
        answer_lines = []
        reasoning_lines = []
        current_section = None
        
        for line in lines:
            line = self.clean_text(line, True)
            if "answer:" in line:
                current_section = "answer"
                line = line.replace("answer:", "")
                print(line)
            elif "inference" in line:
                current_section = "inference"
                line = line.replace("inference:", "")
                print(line)
            
            if current_section == "answer" and line:
                answer_lines.append(line)
            elif current_section == "inference" and line:
                reasoning_lines.append(line)

        inner_speech = " ".join(reasoning_lines)
        output = " ".join(answer_lines)
        
        timestamp = time.time()
        self.memory_DB.insert_new_conversation(None, 
                                            self.triples, 
                                            self.queries, 
                                            inner_speech, 
                                            output, 
                                            None, 
                                            None, 
                                            self.user, 
                                            self.host, 
                                            timestamp,
                                            self.image)
        self.memory_DB.write_history()
        inner_speech = inner_speech.strip().capitalize()
        output = output.strip().capitalize()
        return inner_speech, output
    
    
    def postprocessing(self, output_prompt):
        if self.task == "QA":
            self.inner, self.expressed = self.qa_postprocessing(output_prompt)
            print("\n --- Generated text --- ")
            print("Original output: \n", output_prompt)
            
            print("\nProcessed output: ")
            print("Inner speech: ", self.inner, "\nOutput: ", self.expressed)
            return self.expressed
        
        elif self.task == "PU":
            self.updated_preference = self.preference_postprocessing(output_prompt)
            print("\n --- Updated preference --- ")
            print("Original output: \n", output_prompt)
            print("\nProcessed preference: \n", self.updated_preference)
            
            return self.updated_preference
        
        elif self.task == "AP":
            self.next_action = self.action_postprocessing(output_prompt)
            print("\n --- Predicted Next Action --- ")
            print("Original output: \n", output_prompt)
            print("\nProcessed output: \n", self.next_action)
            
            return self.next_action
        
        elif self.task == "LQ":
            inner, output = self.lq_postprocessing(output_prompt)
            return " "

    
    def extract_json_block(self, text, key):
        """Extracts a JSON-like block under the given key."""
        text = text.replace("'", '"')  # Ensure quotes are double quotes
        pattern = rf'"?{key}"?\s*:\s*\{{.*?\}}(?=,|\n|\Z)'
        match = re.search(pattern, text, re.IGNORECASE | re.DOTALL)
        if match:
            raw = match.group(0)
            raw = raw.replace("'", '"')
            raw = re.sub(r',\s*}', '}', raw)
            try:
                print(raw.split(":", 1)[1].strip())
                return json.loads(raw.split(":", 1)[1].strip())
                
            except json.JSONDecodeError:
                pass
        return {}
    

    def extract_all(self, input_text):
        # Extract blocks
        preference = self.extract_json_block(input_text, "preference")
        facial_emotion = self.extract_json_block(input_text, "facialexpression")
        physiological_emotion = self.extract_json_block(input_text, "physiologicalexpression")
        felt_emotion = self.extract_json_block(input_text, "feltexpression")
        move = self.extract_json_block(input_text, "move")

        return str({
            "preference": preference,
            "emotion": {"facialexpression": facial_emotion,
                        "physiologicalexpression": physiological_emotion,
                        "feltexpression": felt_emotion},
            "move": move
        }).replace("'", '"')
    
    
    def action_postprocessing(self, output):
        output = output.lower()
        json_output = self.extract_all(output)
        
        timestamp = int(time.time() * 1000)
        
        if self.action_DB is None:
            self.action_DB = ActionManagement(self.filepath, self.model_name)
            
        self.action_DB.insert_new_action(self.triples, output, json_output, self.host, timestamp, self.image)
        self.action_DB.write_history()

        return json_output
            
    def preference_postprocessing(self, output):
        """
        post processing process for preference updating
        convert the natural language output to preference matrix
        """
        start = output.find('Updating:') + 9
        end = output.find('Reasoning: ')
        updating = output[start:end]

        # Split into individual triplets
        triplets = [triplet.strip().strip("[").strip("]").strip("'") for triplet in updating.split("', '")]
        preference_list = []
        for triple in triplets: 
            if "|" in triple and "preference towards" in triple:
                preference_list.append(triple)

        timestamp = int(time.time() * 1000)
        self.memory_DB.insert_preference_updating(self.instruction, 
                                            self.triples,
                                            self.queries, 
                                            output,
                                            preference_list,
                                            self.user, 
                                            self.host, 
                                            timestamp)
        self.memory_DB.write_pref_history()

        if (len(preference_list) > 0):         
            return ", ".join(preference_list) 
        else:
            return None
        
    def prompt_formalizing(self, conv_rec=None, conv_rel=None):
        raise NotImplementedError("Subclasses must implement this method.")


    def conversation_summerization(self):
        """
        Summerize the question-answer pair, if their length exceeds a certain length.
        """
        
        if self.history_saving and self.task == "QA":
            
            # get embedding of conversation, and determine whether the summerization is needed
            prompt = T5_summary_prompt.format(input=self.queries, output=self.expressed) 
            embedding = self.get_text_embedding(prompt)
            
            # if the sequence exceeds the limit, then summerize the text
            input_sum_token = self.t5tokenizer(
                f"Summerizaiton: {self.queries}",
                return_tensors="pt", 
                truncation=True
            )
            if self.summerization:
                if len(input_sum_token.input_ids[0])>100:
                    input_summary_output = self.summerize_model.generate(
                        input_ids=input_sum_token.input_ids, 
                        attention_mask=input_sum_token.attention_mask, 
                        max_length=128
                    )
                    print(input_summary_output)
                    #ids = output.encoder_last_hidden_state.mean(dim=1).squeeze()
                    input_summary = self.t5tokenizer.decode(
                        input_summary_output[0],
                        skip_special_tokens=True, 
                    )
                else:
                    input_summary = self.queries
                
                output_sum_token = self.t5tokenizer(
                    f"Summerizaiton: {self.expressed}",
                    return_tensors="pt", 
                    truncation=True
                )
                if len(output_sum_token.input_ids[0])>100:
                    output_summary_output = self.summerize_model.generate(
                        input_ids=output_sum_token.input_ids, 
                        attention_mask=output_sum_token.attention_mask, 
                        max_length=128
                    )
                    print(output_summary_output)

                    output_summary = self.t5tokenizer.decode(
                        output_summary_output[0],
                        skip_special_tokens=True, 
                    )
                else:
                    output_summary = self.expressed
            
            else:
                input_summary = self.queries
                output_summary = self.expressed
                
            print("\n------------------ Summerization step -----------------\n")
            summary = {"input": input_summary, "output": output_summary} #summary_prompt.format(user="input", host="output", input=input_summary, output=output_summary)
            print("Summary: \n", summary)
            print("----------- Conversation turn ends -----------\n")
            timestamp = int(time.time() * 1000)
            self.memory_DB.insert_new_conversation(self.instruction, 
                                                   self.triples, 
                                                   self.queries, 
                                                   self.inner, 
                                                   self.expressed, 
                                                   #self.updated_preference, 
                                                   summary, 
                                                   embedding, 
                                                   self.user, 
                                                   self.host, 
                                                   timestamp,
                                                   self.image)
            self.memory_DB.write_history()
            
    