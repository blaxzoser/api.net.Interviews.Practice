namespace TeeSheet.Dto;

public class ResponseGenericDto : List<T>
{
   
    public int Page { get; set; }
    public List<T> Items { get; set; }

    public int PageSize { get; set; }

    public int TotalCount { get; set; }

}
