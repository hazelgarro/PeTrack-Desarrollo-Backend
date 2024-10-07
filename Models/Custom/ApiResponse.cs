namespace APIPetrack.Models.Custom
{
    public class ApiResponse<T>
    {
        public bool Result { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }
    }
}
