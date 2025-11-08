using System;

namespace LibraryApp.Domain
{
    public class Book : LibraryItem
    {
        public string Author { get; protected set; }
        public string Isbn { get; protected set; }

        public Book(int id, string title, string author, string isbn) : base(id, title)
        {
            Author = string.IsNullOrWhiteSpace(author) ? throw new ArgumentException("Wymagany autor.", nameof(author)) : author;
            Isbn  = string.IsNullOrWhiteSpace(isbn)    ? throw new ArgumentException("Wymagany numer ISBN.", nameof(isbn))     : isbn;
        }

        public override void DisplayInfo()
        {
            Console.WriteLine($"[Książka]  #{Id} \"{Title}\" — {Author} | ISBN: {Isbn} | {(IsAvailable ? "Dostępna" : "Zarezerwowana")}");
        }
    }
}
