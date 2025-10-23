from typing import (
    Literal,
    Sequence,
    TypedDict,
    List,
    Dict
)

def array_to_lines(array):
    res=""
    for elem in array:
        if elem != None:
            res += str(elem)+"\n"
    return res

Role = Literal["system", "user", "assistant"]

class Message(TypedDict):
    role: Role
    content: str

Dialog = Sequence[Message]

class MMMessage(TypedDict):
    role: Role
    content: List[dict]
    
class JanusMessage(TypedDict):
    role: str
    content: str
    images: List[str] 
    
MMDialog = Sequence[MMMessage]