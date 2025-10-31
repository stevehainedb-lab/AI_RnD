namespace MQR.WebAPI.ServiceModel;

public class QueryResultField
{
   public string Identifier { get; set; }
      
   public string Value { get; set; }
   
   public override string ToString() => $"{Identifier}: {Value}";
}