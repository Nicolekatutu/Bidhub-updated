namespace Bidhub.Dto
{
    public class ProductDocDTO
    {
        //public string DocumentType { get; set; }

        public int ProductId { get; set; }
        public IFormFile DocumentUrl { get; set; }


    }
    public class ProductRtnDocDTO
    {
        //public string DocumentType { get; set; }

        public string DocumentType { get; set; }

        public string DocumentUrl { get; set; }
        public int ProductId { get; set; }



    }
}