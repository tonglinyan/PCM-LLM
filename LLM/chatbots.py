from basechatbot import BaseChatBot
import torch
import time
from transformers import T5Tokenizer, T5ForConditionalGeneration, pipeline, AutoTokenizer, AutoModelForCausalLM
from utils import *
from memory_management import *
from vllm import LLM, SamplingParams

def create_chatbot(model_name: str, model_size: str, summerization: bool, history_saving: bool, history_calling: bool, multimodal: bool, language: str = "fr"):
    if "llama" in model_name:
        return LlamaChatBot(model_name, model_size, summerization, history_saving, history_calling, multimodal, language)
    elif "qwen" in model_name:
        return Qwen251MChatBot(model_name, model_size, summerization, history_saving, history_calling, multimodal, language)
    else:
        raise ValueError(f"Unsupported model: {model_name}. Only 'llama3.1' and 'qwen2.5' are supported.")

class LlamaChatBot(BaseChatBot):
    
    def load_models(self, model_size: str):
        version = self.model_name[5:]
        self.model_id = f"deepseek-ai/DeepSeek-R1-Distill-Llama-8B" if "distill" in self.model_name else f"meta-llama/Meta-Llama-{version}-{model_size}-Instruct"
        self.pipeline = pipeline(
            "text-generation",
            model=self.model_id,
            model_kwargs={"torch_dtype": torch.bfloat16},
            device_map="auto",
        )
        self.t5tokenizer = T5Tokenizer.from_pretrained("google-t5/t5-base")
        self.summerize_model = T5ForConditionalGeneration.from_pretrained("google-t5/t5-base")
        self.summerize_model.eval()
        print(self.model_id, " is loaded.")
        
        
        if self.language == "fr":         
            self.translation_model = T5ForConditionalGeneration.from_pretrained("google-t5/t5-base")
            self.translation_model.eval()
        

    def inference(self, input_pcm, max_gen_len=512):
        input_prompt = self.prompt_preprocessing(input_pcm)
        print("\n------------------ Inference step -----------------\n")
        start_time = time.time()
        
        print("input prompt:")
        print(input_prompt, "\n")   
        
        prompt = self.pipeline.tokenizer.apply_chat_template(
            input_prompt, 
            tokenize=False, 
            add_generation_prompt=True
            )
        
        terminators = [
            self.pipeline.tokenizer.eos_token_id,
            self.pipeline.tokenizer.convert_tokens_to_ids("<|eot_id|>")
        ]
        
        output = self.pipeline(
            prompt,
            max_new_tokens=max_gen_len,
            eos_token_id=terminators,
            do_sample=True,
            temperature=0.6,
            top_p=0.9
        )
        output_prompt = output[0]["generated_text"][len(prompt):]
        
        print(output_prompt)   
        print("Inference time: \n", time.time()-start_time)     

        return self.postprocessing(output_prompt)
    
    
    def _append_message(self, dialogs: list, role: str, content: str) -> None:
        """Helper function to append message to dialogs."""
        if content is not None:
            dialogs.append(Message(role=role, content=content))


    def prompt_formalizing(self, conv_rec=None, conv_rel=None) -> list:
        """Construct dialog sequence from available components."""
        dialogs = []
        
        # Add conversation history - summaries
        if conv_rel:
            for conversation in conv_rel:
                self._append_message(dialogs, "user", conversation["summary"]["input"])
                self._append_message(dialogs, "assistant", conversation["summary"]["output"])
        
        # Add conversation history - recommendations
        if conv_rec:
            for conversation in conv_rec:
                self._append_message(dialogs, "user", conversation["query"])
                self._append_message(dialogs, "assistant", conversation["output"])
        
        instruction = self.instruction + "\n" if self.instruction != "None" else ""
        queries = f"'query': {self.queries}\n" if self.queries != "None" else ""
        triples = f"'belief': {self.paranthese_adding(self.triples)}\n" if self.triples != "None" else ""
        
        query_content = instruction + triples + queries
        self._append_message(dialogs, "user", query_content)

        return dialogs


class Qwen251MChatBot(LlamaChatBot):
    def load_models(self, model_size: str):
        self.model_id = "Qwen/Qwen2.5-7B-Instruct-1M"
        self.tokenizer = AutoTokenizer.from_pretrained(self.model_id)
        self.sampling_params = SamplingParams(temperature=0.6, top_p=0.9, repetition_penalty=1.05, max_tokens=512)

        self.model = AutoModelForCausalLM.from_pretrained(
            self.model_id,
            torch_dtype="auto",
            device_map="auto"
        )
        self.tokenizer = AutoTokenizer.from_pretrained(self.model_id)
        self.t5tokenizer = T5Tokenizer.from_pretrained("google-t5/t5-base")
        self.summerize_model = T5ForConditionalGeneration.from_pretrained("google-t5/t5-base")
        self.summerize_model.eval()
        print(self.model_id, " is loaded.")
        
        
        if self.language == "fr":         
            self.translation_model = T5ForConditionalGeneration.from_pretrained("google-t5/t5-base")
            self.translation_model.eval()
        

    def inference(self, input_pcm, max_gen_len=512):
        input_prompt = self.prompt_preprocessing(input_pcm)
        print("\n------------------ Inference step -----------------\n")
        start_time = time.time()
        
        print("input prompt:")
        print(input_prompt, "\n")   
        
        text = self.tokenizer.apply_chat_template(
            input_prompt,
            tokenize=False,
            add_generation_prompt=True
        )
        model_inputs = self.tokenizer([text], return_tensors="pt").to(self.model.device)

        generated_ids = self.model.generate(
            **model_inputs,
            max_new_tokens=4096
        )
        generated_ids = [
            output_ids[len(input_ids):] for input_ids, output_ids in zip(model_inputs.input_ids, generated_ids)
        ]
        
        output_prompt = self.tokenizer.batch_decode(generated_ids, skip_special_tokens=True)[0]
        
        print(output_prompt)   
        print("Inference time: \n", time.time()-start_time)     
        
        return self.postprocessing(output_prompt)