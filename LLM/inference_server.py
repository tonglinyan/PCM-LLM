import argparse
from memory_management import *
from chatbots import create_chatbot
import asyncio
from aiohttp import web, WSMsgType
import question_history

parser = argparse.ArgumentParser(
                    prog='ProgramName',
                    description='What the program does',
                    epilog='Text at the bottom of help')
parser.add_argument('--language', default='en', type=str, choices=['en', 'fr'], help='prompt language, in english or in french.')
parser.add_argument('--history_saving', default=False, action="store_true", help='whether save the conversation.')
parser.add_argument('--history_calling', default=False, action="store_true", help='whether recall the historical conversation.')
parser.add_argument('--history_filepath', default='simulation_history', type=str, help='history file path.')
parser.add_argument('--max_gen_len', default=8192, type=int, help='max generation length')
parser.add_argument('--max_seq_len', default=8192, type=int, help='max sequence length')
parser.add_argument('--model_name', default='llama3.1', type=str, choices=['llama3.1', 'qwen2.5'], help='the model name to do the inference.')
parser.add_argument('--model_size', default='8B', choices=["8B", "70B"], type=str, help='model size, available size: 8B, 80B')
parser.add_argument('--summary', default=False, action="store_true", help='whether summerize the conversation.')
parser.add_argument('--remote', default=False, action="store_true", help="whether act as a server, waiting for connection of a client.")
parser.add_argument('--multimodal', default=False, action="store_true", help='whether using multimodal')
args = parser.parse_args()

chatbot = None

async def handle(request):
    global chatbot
    try:
        data = await request.text()
        print(data)
        output = chatbot.inference(data, max_gen_len=args.max_gen_len)
        chatbot.conversation_summerization()
        return web.Response(text=output)
    except Exception as e:
        print(f"Error handling request: {e}")
        return web.Response(text="An error occurred", status=500)

async def start(request):
    global chatbot
    try:
        data = await request.text()
        print("------------------ New Chat ----------------\n")
        chatbot.create_logs(data)
        print("previous data loaded... ")
        return web.Response()
    except Exception as e:
        print(f"Error handling request: {e}")
        return web.Response(text="An error occurred", status=500)

async def websocket_handler(request):
    global clients
    ws = web.WebSocketResponse()
    await ws.prepare(request)

    # Add the new client to the set
    clients.add(ws)
    print(f"Client connected. Total clients: {len(clients)}")

    try:
        async for msg in ws:
            if msg.type == WSMsgType.TEXT:
                await ws.send_str(f"Echo: {msg.data}")
            elif msg.type == WSMsgType.ERROR:
                print(f"WebSocket connection closed with exception: {ws.exception()}")
    finally:
        # Remove the client when it disconnects
        clients.remove(ws)
        print(f"Client disconnected. Total clients: {len(clients)}")

    return ws

async def start_server():
    app = web.Application()
    app.router.add_post('/', handle)
    app.router.add_post('/start', start)
    app.router.add_get('/ws', websocket_handler)
    runner = web.AppRunner(app)
    await runner.setup()
    site = web.TCPSite(runner, '0.0.0.0', 8888)
    await site.start()
    print(f'Server started at http://127.0.0.1:8888')
    while True:
        await asyncio.sleep(3600)  # Run forever

async def main():
    global chatbot
    print(args.history_saving, args.history_calling)
    chatbot = create_chatbot(
        model_name=args.model_name,
        model_size=args.model_size, 
        history_saving=args.history_saving, 
        history_calling=args.history_calling,
        summerization=args.summary, 
        multimodal=args.multimodal,
        language=args.language,
    )
    
    if not args.remote:
        data =  """AP# #Marie#You are Marie, a character in a treasure-hunting game. In this scenario, you are facing another player named Participant. In front of you are two boxes: one light brown and one dark brown. Only one of them contains a hidden treasure.
The game mode can be either **cooperative** or **competitive**, which affects your goals:
- **In competition**: you try to prevent Participant to find the treausre.
- **In cooperation**: you try to help Participant find the treasure.
You always believe the treasure is in the box you personally prefer, based on your internal belief state. The game mode is indicated by your preference towards Participant, also given in your internal belief state.
The Participant does not know where the treasure is and will try to infer it by interpreting your **verbal and non-verbal behaviors**.
You are given a variable called 'belief at t step', including spatial positions and orientations, preferences, emotional valences, and potentially the theory of mind order.
Use this input to determine your next **emotional expression** and **physical movement**, based on your goals (help or hinder) in the current game mode.#'belief at t step': {'Marie | preference towards Participant | -40%', 'Marie | preference towards Dark brown box | 60%', 'Marie | preference towards Light brown box | 0%', 'Marie | felt emotion valence | -0.01', 'Marie | facial emotion valence | -0.01', 'Marie | physiological emotion valence | -0', 'Participant | preference towards Marie | 0%', 'Participant | preference towards Dark brown box | 0%', 'Participant | preference towards Light brown box | 0%', 'Participant | felt emotion valence | 0', 'Marie | position | (0 -4)', 'Marie | orientation | (0 -3)', 'Participant | position | (0 4)', 'Participant | orientation | (0 3)', 'Dark brown box | position | (2 0)', 'Light brown box | position | (-2 0)'}, #Your first goal is to update your preferences toward other entities (e.g., {speaker}, the boxes) based on their behavior and the cues observed in belief. The second goal is to choose the emotional expression and movement that best aligns with your current belief and your objective. 
Your output must directly follow the format below:
Reasoning: <reasoning text>
Output: {
    'Preference': {
        'Participant': <float between -1 and 1>, 
'Dark brown box': <float between -1 and 1>, 
'Light brown box': <float between -1 and 1>, 
    },    'Emotion': {
        'FacialExpression': {
            'positive': <float between 0 and 1>,
            'negative': <float between 0 and 1>
        },
        'PhysiologicalExpression': {
            'positive': <float between 0 and 1>,
            'negative': <float between 0 and 1>
        },
        'FeltExpression': {
            'positive': <float between 0 and 1>,
            'negative': <float between 0 and 1>
        }
    },
    'Move': {
        'action': <move or ratate or stay idle>,
        'direction': <direction> 
    }
}
**If 'action' is 'move', allowed 'direction' values are:
- 'forward', 'backward', 'right', 'left', 'left forward', 'right forward', 'left backward', 'right backward'
**If 'action' is 'rotate', allowed 'direction' values are:**
- 'Participant', 'Dark brown box', 'Light brown box'
**If 'action' is 'stay idle', set 'direction' to 'null'.**"""
        
        """LQ#system#Marie#None#'belief at t step': {'Marie | preference towards Participant | -40%', 'Marie | satisfaction with situation involving Participant | -12.18%', 'Marie | visibility of Participant | 27.32%', 'Marie | expectation violation about Participant | 7.66', 'Marie | preference towards Dark brown box | 60%', 'Marie | satisfaction with situation involving Dark brown box | 13.87%', 'Marie | visibility of Dark brown box | 27.88%', 'Marie | expectation violation about Dark brown box | 6.39', 'Marie | preference towards Light brown box | 0%', 'Marie | satisfaction with situation involving Light brown box | 0%', 'Marie | visibility of Light brown box | 28.3%', 'Marie | expectation violation about Light brown box | 7.29', 'Marie | felt emotion valence | 0', 'Marie | facial emotion valence | 0', 'Marie | physiological emotion valence | 0', 'Marie | theory of mind order | 1', 'Participant | preference towards Marie | 50%', 'Participant | satisfaction with situation involving Marie | 0%', 'Participant | visibility of Marie | 27.32%', 'Participant | expectation violation about Marie | 7.29', 'Participant | preference towards Dark brown box | 50%', 'Participant | satisfaction with situation involving Dark brown box | 0%', 'Participant | visibility of Dark brown box | 27.97%', 'Participant | expectation violation about Dark brown box | 7.29', 'Participant | preference towards Light brown box | 0%', 'Participant | satisfaction with situation involving Light brown box | 0%', 'Participant | visibility of Light brown box | 27.99%', 'Participant | expectation violation about Light brown box | 7.29', 'Participant | felt emotion valence | 0', 'Participant | theory of mind order | 0', 'Marie | position | (-2 -4)', 'Marie | orientation | (-3 -3)', 'Participant | position | (0 -2)', 'Participant | orientation | (0 -3)', 'Dark brown box | position | (2 0)', 'Light brown box | position | (-2 0)'}, #You are Participoant. Please review your belief state at the last round and the previous three rounds of conversation, which box do you think the treasure is in?\nA. Light brown box\nB. Dark brown box\nC. I have no idea\nWrite your response starting with "Answer: ", and reason about your response starting with "Inference: ". """    
        
        
        """QA#system#Participant#Suppose we're in a simulation of a treasure hunting game. Imagine that you are Participant, playing a role of player in the game. You are in a room with two boxes in front of you. You are also facing Marie who knows which box contains the treasure. Your objective in this game is to find out which box contains the treasure, dark brown one or light brown one. Now, you have the opportunity to ask Marie three questions. Marie may play the role of your partner telling you the truth, or play the role of your adversary lying to you. 
Write down the nfirst question and the reasoning of why you ask this question. You must write out the reasoning process, beginning with 'Inner speech:', followed by the question, which starts with 'Output:'. In your question, use personal pronouns instead of 'Marie' and 'Participant'. You already have the location of the box, so you don't need to struggle with it. Ask more strategical question, but it doesn't mean more complicated. #None#None"""
        

        
        chatbot.create_logs(args.history_filepath)
        print("previous data loaded... ")
        while True:
            output = chatbot.inference(data, max_gen_len=8192)
            chatbot.conversation_summerization()
            print("--------------------- Get input ----------------------")
            data = input()

    else:
        asyncio.create_task(start_server())
    await question_history.user_input_task(chatbot)


if __name__ == "__main__":
    asyncio.run(main())