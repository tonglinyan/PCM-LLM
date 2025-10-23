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
        data =  """ """
        
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