namespace PCM.Verbal
{
    public class Message
    {
        public string _role;
        public string _content;

        public Message(string role, string content)
        {
            _role = role;
            _content = content;
        }
    }
    public class Dialog
    {
        List<Message> _messages;
        // Start is called before the first frame update
        public Dialog()
        {
            _messages = new List<Message>();
        }

        // Update is called once per frame
        public void AddNewMessage(string role, string content)
        {
            Message new_message = new(role, content);
            _messages.Add(new_message);
        }

        public void GetLastMessage(out string text, out string speaker)
        {
            if (_messages != null && _messages.Count > 0)
            {
                text = _messages[_messages.Count - 1]._content;
                speaker = _messages[_messages.Count - 1]._role;
            }
            else
            {
                text = null;
                speaker = null;
            }
        }

        public Dialog CopyDialog()
        {
            Dialog new_dialog = new Dialog();
            foreach (Message m in _messages)
            {
                new_dialog.AddNewMessage(m._role, m._content);
            }
            return new_dialog;
        }

        public void PrintDialog()
        {
            foreach (Message m in _messages)
            {
                Console.WriteLine($"{m._role}: " + m._content);
            }
        }
    }
}

