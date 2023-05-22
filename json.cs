using System.Text;

namespace json_csharp;

public class JSON {

    private enum States {
        KeyFinding = 2,
        KeyReading = 3,
        ValueDetermining = 4,
        StringValueReading = 5,
        NotStringValueReading = 6,
        ValueFinding = 7,
        SavingField = 8,
        SavingCollection = 9,
    }

    public List<JSONRecord> Values;

    private JSON(){
        Values = new List<JSONRecord>();
    }

    public static JSON ParseFrom(string json){
        
        
        bool include = false;
        string banned = "\t\n\r\" ";
        StringBuilder formattedJson = new StringBuilder();
        for (int i = 0; i < json.Length; i++){
            char symbol = json[i];
            if (symbol == '\"' && !(json[i-1] == '\\')){
                include = !include;
                formattedJson.Append(symbol);
            }
            else if (!(banned.Contains(symbol) && !include)){
                formattedJson.Append(symbol);
            }
        }

        int id = 0;
        //Console.WriteLine(formattedJson);
    
        var result = new JSON();
        JSONRecord builtNodeNow = null;

        
        void SetNodeAsCollection(bool arrayFlag){
            if (builtNodeNow != null){
                builtNodeNow.ArrayFlag = arrayFlag;
                SetKeyId();
            }           
        }
        void SetNodeAsField(){
            if (builtNodeNow != null){
                builtNodeNow.ArrayFlag = null;
                SetKeyId();
            }  
        }

        void ProcessNext(){
            if (builtNodeNow != null){
                builtNodeNow = new JSONRecord() {Parent = builtNodeNow};
            }
            else{
                builtNodeNow = new JSONRecord() {Parent = null};
            }
        }
        void Save(){
            if (builtNodeNow!= null){
                result.Values.Add(builtNodeNow);
            }
        }
        void StepBack(int count){
            for (int i = 0;i < count; i++){
                if (builtNodeNow.Parent != null){
                    builtNodeNow = builtNodeNow.Parent;
                }       
            }
        }
        // временно
        void SetKeyId(){
            if (builtNodeNow!=null && builtNodeNow.Key == String.Empty){
                builtNodeNow.Key = (++id).ToString();
            }
        }

        States currentState = States.KeyFinding;
        for (int i = 0; i<formattedJson.Length; i++){
            char symbol = formattedJson[i];
            switch (currentState) {
                case States.KeyFinding:
                    if (symbol == '\"'){
                        ProcessNext();
                        Save();   
                        currentState = States.KeyReading;
                    }
                    else if (char.IsDigit(symbol) || symbol == '-' || "nft".Contains(symbol)){
                        ProcessNext();
                        Save();
                        i--;
                        currentState = States.NotStringValueReading;
                    }
                    else if (symbol == '}' || symbol == ']'){
                        StepBack(1);
                        Console.WriteLine(" - " + builtNodeNow);
                    }
                    else if (symbol == '{'){
                        ProcessNext();
                        Save();
                        SetNodeAsCollection(false);
                    }
                    else if (symbol == '['){
                        ProcessNext();
                        Save();
                        SetNodeAsCollection(true);
                    }
                    break;
                case States.KeyReading:
                    if (symbol == '\"'){
                        currentState = States.ValueFinding;
                    }
                    else {
                        builtNodeNow.Key += symbol;
                    }
                    break;
                case States.ValueFinding:
                    if (symbol == ':'){
                        currentState = States.ValueDetermining;
                    }
                    else if (symbol == ',') {
                        builtNodeNow.Value = builtNodeNow.Key;
                        SetNodeAsField();
                        builtNodeNow.Key = string.Empty;
                        StepBack(1);
                        currentState = States.KeyFinding;
                    }
                    else if (symbol == ']') {
                        StepBack(2);
                        Console.WriteLine(" + " + builtNodeNow);
                        currentState = States.KeyFinding;
                    }
                    break;
                
                case States.ValueDetermining:
                    if (symbol == '[') {
                        SetNodeAsCollection(true);
                        currentState = States.KeyFinding;
                    }
                    else if (symbol == '{'){
                        SetNodeAsCollection(false);
                        currentState = States.KeyFinding;
                    }
                    else if (symbol == '\"'){
                        SetNodeAsField();
                        currentState = States.StringValueReading;
                    }
                    else {
                        SetNodeAsField();
                        i--;
                        currentState = States.NotStringValueReading;
                    }
                    break;
                case States.StringValueReading:
                    if (symbol == '\"'){
                        StepBack(1);
                        currentState = States.KeyFinding;
                    }
                    else {
                        builtNodeNow.Value += symbol;   
                    }
                    break;
                case States.NotStringValueReading:
                    if (symbol == ',' ){
                        StepBack(1);
                        currentState = States.KeyFinding;    
                    }
                    else if (symbol == ']' || symbol == '}'){
                        StepBack(2);
                        currentState = States.KeyFinding;
                    }
                    else {
                        builtNodeNow.Value += symbol;   
                    }
                    break;                
                
                default:
                    break;
            }
        }
        return result;
    }

    public class JSONRecord {

        public string Key = string.Empty;
        public string? Value = null;
        public bool? ArrayFlag = null;
        public JSONRecord? Parent;
        public bool? GetParentFlag(){
            if (Parent == null){
                return false;
            }
            else {
                return Parent.ArrayFlag;
            }
        }

        public override string ToString()
        {
            return "P_key: " + (Parent == null ? "no_parent" : Parent.Key) + " key: " + Key + " value: " + 
            (Value == null ? "no_value" : Value) + " Type: " + (ArrayFlag == null ? "field" : (ArrayFlag == true ? "array" : "obj"));
        }
    }

    public override string ToString()
    {
        return String.Join("\n", Values.Select(x => x.ToString()));
    }

    public static string ParseFromObjectModel(JSON model){
        StringBuilder result = new StringBuilder();
        Stack<JSONRecord> parentStack = new Stack<JSONRecord>();

        void TryClose(JSONRecord currentParent){
            while(parentStack.Any() && parentStack.Peek() != currentParent){
                CloseAnyway();
            }
        }
        void CloseAnyway(){
            var closing = parentStack.Pop();
                char close = closing.ArrayFlag == true ? ']' : '}';
                int endIndex = result.Length - 2;
                if(result.Length > 0 && result[endIndex] == ','){
                    result[endIndex] = '\n';
                }
                else{
                    result.Append('\n');
                }
                result.Append(close + "\n");
        }

        foreach (var tuple in model.Values){
            if (tuple.Parent != null){
                TryClose(tuple.Parent);
            }
            if (tuple.ArrayFlag == null){
                if (tuple.Key != string.Empty){
                    result.Append('\"' + tuple.Key + '\"');
                    if (tuple.Value != null){
                        result.Append(':');
                    }
                }
                if (tuple.Value != null){    
                    if (tuple.Value == string.Empty){
                        result.Append("\"\"");
                    }
                    else if (tuple.Value == "false" || tuple.Value == "true" || tuple.Value == "null"){
                        result.Append(tuple.Value);
                    }
                    else if (double.TryParse(tuple.Value, out double t)){
                        result.Append(tuple.Value);
                    }
                    else{ 
                        result.Append('\"' + tuple.Value + '\"');
                    }
                    result.Append(",\n");
                }
            }
            else {
                parentStack.Push(tuple);
                char open = tuple.ArrayFlag == true ? '[' : '{';
                if (!char.IsDigit(tuple.Key[0])){
                    result.Append('\"' + tuple.Key + "\":\n");
                }
                result.Append(open + "\n");
            }
        }
        while (parentStack.Any()){
            CloseAnyway();
        }
        return result.ToString().Replace("\n\n", "\n"); 
    }
        
}

