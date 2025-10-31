namespace MQR.WebAPI.ServiceModel;

public class QueryResultRow
{
   public string Identifier { get; set; }

   public string? FullLine { get; set; }

   public QueryResultField []  Fields { get; set; }

   public override string ToString() => Identifier;
}