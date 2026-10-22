namespace TeeSheet.Dto;

public class TeeTimeDto
{
      public int Id { get; set; }

       public string CourseName { get; set; }

       public DateTime StartUtc { get; set; }

       public DateTime StartLocal { get; set; }
       public int SpotsLeft { get; set; }
       public decimal Price { get; set; }
        public string Currency  { get; set; }

}
