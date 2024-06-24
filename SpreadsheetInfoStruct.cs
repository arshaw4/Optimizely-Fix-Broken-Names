namespace NoImageCheck
{
    public class SpreadsheetInfoStruct
    {
        public string erpNumber {  get; set; }
        public string name { get; set; }
        public string newName { get; set; }
        public string newKeyword { get; set; }
        public string id { get; set; }

        public override string ToString()
        {
            return $"{erpNumber}: {name}: {newName}: {newKeyword}: {id}";
        }
    }
}
