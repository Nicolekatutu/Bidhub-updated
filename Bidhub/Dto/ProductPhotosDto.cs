namespace Bidhub.Dto
{
    public class ProductPhotosDTO
    {
        public int ProductId { get; set; }
        public IFormFile PhotoUrl { get; set; }
    }
    public class ProductRtnPhotoDTO
    {
        public int ProductId { get; set; }
        public string PhotoUrl { get; set; }
    }
}