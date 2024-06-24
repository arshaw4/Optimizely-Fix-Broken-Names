namespace SearchQueryKeywordFixes
{
    struct Settings
    {
        static public string baseURL = "https://www.company.com";
        static public string identURL = baseURL + "/identity/connect/token";
        static public string Products = baseURL + "/api/v1/admin/products";
        static public string savePath = "C:\\product_changes.txt";
        static public string authForToken = "Basic aXNjX2FkbWluOkY2ODRGQzk0LUIzQkUtNEJDNy1COTI0LTYzNjU2MTE3N0M4Rg==";
        //Acquired by base64 encoding authentication from https://docs.developers.optimizely.com/configured-commerce/reference/admin-api-architecture
    }
}
