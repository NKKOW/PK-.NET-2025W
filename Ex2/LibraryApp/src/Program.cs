﻿using System;
using LibraryApp.Domain;
using LibraryApp.Services;

namespace LibraryApp
{
    class Program
    {
        static void Main()
        {
            var library = new LibraryService();
            var analytics = new AnalyticsService(library);

            library.OnNewReservation += r =>
                Console.WriteLine($"[INFO] Nowa rezerwacja: {r.Item.Title} dla {r.UserEmail} ({r.From:yyyy-MM-dd} → {r.To:yyyy-MM-dd})");
            library.OnReservationCancelled += r =>
                Console.WriteLine($"[INFO] Anulowano rezerwację #{r.Id} ({r.Item.Title}) dla {r.UserEmail}");

            while (true)
            {
                Console.WriteLine("\n=== System Biblioteczny ===");
                Console.WriteLine("1. Dodaj książkę");
                Console.WriteLine("2. Dodaj e-booka");
                Console.WriteLine("3. Zarejestruj użytkownika");
                Console.WriteLine("4. Zarezerwuj pozycję");
                Console.WriteLine("5. Anuluj rezerwację");
                Console.WriteLine("6. Pokaż dostępne pozycje (filtr frazą)");
                Console.WriteLine("7. Moje rezerwacje");
                Console.WriteLine("8. Statystyki");
                Console.WriteLine("0. Wyjście");
                Console.Write("> ");

                var choice = Console.ReadLine();
                try
                {
                    switch (choice)
                    {
                        case "1":
                            Console.Write("Tytuł: "); var title = Console.ReadLine();
                            Console.Write("Autor: "); var author = Console.ReadLine();
                            Console.Write("ISBN: "); var isbn = Console.ReadLine();
                            library.AddItem(new Book(library.NextId(), title!, author!, isbn!));
                            Console.WriteLine("Dodano książkę.");
                            break;

                        case "2":
                            Console.Write("Tytuł: "); var t = Console.ReadLine();
                            Console.Write("Autor: "); var a = Console.ReadLine();
                            Console.Write("ISBN: "); var i = Console.ReadLine();
                            Console.Write("Format (PDF/EPUB): "); var f = Console.ReadLine();
                            library.AddItem(new EBook(library.NextId(), t!, a!, i!, f!));
                            Console.WriteLine("Dodano e-booka.");
                            break;

                        case "3":
                            Console.Write("Email użytkownika: "); var email = Console.ReadLine();
                            library.RegisterUser(email!);
                            Console.WriteLine("Zarejestrowano użytkownika.");
                            break;

                        case "4":
                            Console.Write("ID pozycji: "); int id = int.Parse(Console.ReadLine()!);
                            Console.Write("Email: "); var u = Console.ReadLine();
                            Console.Write("Od (yyyy-MM-dd): "); var from = DateTime.Parse(Console.ReadLine()!);
                            Console.Write("Do (yyyy-MM-dd): "); var to = DateTime.Parse(Console.ReadLine()!);
                            var res = library.CreateReservation(id, u!, from, to);
                            Console.WriteLine($"Utworzono rezerwację #{res.Id}.");
                            break;

                        case "5":
                            Console.Write("ID rezerwacji do anulowania: "); int rid = int.Parse(Console.ReadLine()!);
                            library.CancelReservation(rid);
                            break;

                        case "6":
                            Console.Write("Fraza (tytuł/autor, puste = tylko dostępne bez filtra): ");
                            var phrase = Console.ReadLine();
                            foreach (var it in library.ListAvailableItems(i =>
                                         string.IsNullOrWhiteSpace(phrase) ? true :
                                         i.Title.Contains(phrase!, StringComparison.OrdinalIgnoreCase) ||
                                         (i is Book b && b.Author.Contains(phrase!, StringComparison.OrdinalIgnoreCase))))
                                it.DisplayInfo();
                            break;

                        case "7":
                            Console.Write("Email: "); var ue = Console.ReadLine();
                            foreach (var r in library.GetUserReservations(ue!)) Console.WriteLine(r);
                            break;

                        case "8":
                            Console.WriteLine($"Średni czas wypożyczenia: {analytics.AverageLoanLengthDays():F2} dni");
                            Console.WriteLine($"Najpopularniejszy tytuł: {analytics.MostPopularItemTitle()}");
                            Console.WriteLine($"Łączna liczba rezerwacji: {analytics.TotalLoans()}");
                            Console.WriteLine($"Wskaźnik realizacji (nieanulowane): {analytics.FulfillmentRate():P1}");
                            break;

                        case "0":
                            return;

                        default:
                            Console.WriteLine("Nieznana opcja.");
                            break;
                    }
                }
                catch (ReservationConflictException ex)
                {
                    Console.WriteLine("[KOLIZJA] " + ex.Message);
                }
                catch (ArgumentException ex)
                {
                    Console.WriteLine("[BŁĄD] " + ex.Message);
                }
                catch (InvalidOperationException ex)
                {
                    Console.WriteLine("[BŁĄD] " + ex.Message);
                }
                catch (FormatException)
                {
                    Console.WriteLine("[BŁĄD] Nieprawidłowy format danych wejściowych.");
                }
            }
        }
    }
}
