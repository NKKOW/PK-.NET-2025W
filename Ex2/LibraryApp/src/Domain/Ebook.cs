using System;

namespace LibraryApp.Domain
{
    public class EBook : Book
    {
        public string FileFormat { get; protected set; } // PDF/EPUB/MOBI

        public EBook(int id, string title, string author, string isbn, string fileFormat)
            : base(id, title, author, isbn)
        {
            FileFormat = string.IsNullOrWhiteSpace(fileFormat) ? throw new ArgumentException("Wymagany format pliku.", nameof(fileFormat)) : fileFormat;
        }

        public override void DisplayInfo()
        {
            Console.WriteLine($"[E-książka] #{Id} \"{Title}\" — {Author} | ISBN: {Isbn} | Format: {FileFormat} | {(IsAvailable ? "Dostępna" : "Zarezerwowana")}");
        }
    }
}
