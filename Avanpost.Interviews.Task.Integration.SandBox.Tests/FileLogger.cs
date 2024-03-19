
using Avanpost.Interviews.Task.Integration.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avanpost.Interviews.Task.Integration.SandBox.Tests
{
    public class FileLogger : ILogger
    {
        string _fileName;
        string _connectorName;

        public FileLogger(string fileName, string connectorName)
        {
            _fileName = fileName;
            _connectorName = connectorName;
        }
        void Append(string text)
        {
            using (var sw = File.AppendText(_fileName))
                sw.WriteLine(text);
        }
        public void Debug(string message) => Append($"{DateTime.Now.ToString()}:{_connectorName}:DEBUG:{message}");

        public void Error(string message) => Append($"{DateTime.Now.ToString()}:{_connectorName}:ERROR:{message}");
        public void Warn(string message) => Append($"{DateTime.Now.ToString()}:{_connectorName}:WARNING{message}");

    }
}
