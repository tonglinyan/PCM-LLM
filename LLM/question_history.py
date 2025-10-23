import aioconsole

args = None

async def user_input_task(chatbot):
    global args
    while True:
        print("question \"host\" \"conversation turn\" \"prompt\"")
        user_input = await aioconsole.ainput(">")
        command_string = user_input
        args = command_string.split()
        if len(args) > 3 and args[0] == 'question':
            host, x = args[1], int(args[2])
            prompt = " ".join(args[3:])
            history = chatbot.memory_DB.history
            messages = history[host]["system"][0]
            tmp_messages = messages[:]
            for i in range (x + 1, len(messages)):
                tmp_messages.remove(messages[i])
            chatbot.memory_DB.history[host]["system"][0] = tmp_messages
            _, expressed, _ = chatbot.inference(f"system#{host}###{prompt}", max_gen_len=2048)
            chatbot.memory_DB.history[host]["system"][0] = messages
            chatbot.conversation_summerization()
            break
    await user_input_task(chatbot)
            

