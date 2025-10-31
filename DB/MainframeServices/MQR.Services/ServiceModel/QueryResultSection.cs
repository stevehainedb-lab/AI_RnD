namespace MQR.WebAPI.ServiceModel;

public class QueryResultSection
{
   public string Identifier { get; set; }
   
   public QueryResultRow[] Rows { set; get; }
   
   public override string ToString() => Identifier;
}