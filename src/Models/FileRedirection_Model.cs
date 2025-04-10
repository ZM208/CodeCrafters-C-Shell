using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace src.Models
{
    public class FileRedirection_Model
    {
        public FileRedirection_Model(List<string> userInputs, bool create, int indexOfRedirection)
        {
            FileMode = create ? FileMode.Create : FileMode.Open;
            Output = new StringBuilder();
            FileLocation = userInputs[indexOfRedirection + 2];
        }
        public void EndRedirection()
        {
            var writer = new StreamWriter(FileLocation, append: FileMode == FileMode.Open) { AutoFlush = true };
            writer.Write(Output.ToString());
            writer.Close();
        }
        public string FileLocation { get; set; }
        public StringBuilder Output { get; set; }
        public FileMode FileMode { get; set; } = FileMode.Create;
    }
}
