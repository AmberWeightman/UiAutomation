using System;
using System.Linq;

namespace UiAutomation.Logic.Workflows
{
    public enum MessageType
    {
        Error,
        Warning
    }

    public class Message
    {
        public MessageType Type { get; }

        public string Code { get; }

        /// <summary>
        /// OPTIONAL OrderId with which this message is associated.
        /// </summary>
        public string OrderId { get; }

        private string _text;

        private object[] _args;

        public string Text => ToString();

        private Message(MessageType type, string code, string orderId, string text, object[] args)
        {
            Type = type;
            Code = code;
            OrderId = orderId;
            _text = text;
            _args = args;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="code"></param>
        /// <param name="args">The first argument must ALWAYS be the orderId (or null if orderId not applicable).</param>
        /// <returns></returns>
        public static Message Create(MessageType type, string code, object[] args = null)
        {
            string orderId = null;
            if (args != null && args.Any())
            {
                orderId = args[0].ToString(); 
                args = args.Count() == 1 ? null : args.Skip(1).ToArray(); // Remove orderId from the args
            }

            switch (code)
            {
                case "ERR001":
                    return new Message(type, code, orderId, "No records were found in Landonline which matched the selected CT '{0}'.", args);
                case "ERR002":
                    return new Message(type, code, orderId, "Could not find image {0} with minimum accuracy {1}.", args);
                case "WAR001":
                    return new Message(type, code, orderId, "Citrix client is unavailable.", args);
                case "WAR002":
                    return new Message(type, code, orderId, "Event '{0}' completed with accuracy {1}.", args);
                default:
                    throw new ApplicationException($"Error code {code} is not recognised.");
            }
        }

        public override string ToString()
        {
            return (_args == null || !_args.Any()) ? _text : string.Format(_text, _args);
        }
    }
    
}
