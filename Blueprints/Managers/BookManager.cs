using BeautifyEffect;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Blueprints
{
    public static class BookManager
    {
        // Objects & Variables
        public static int currentBookId;

        private static Dictionary<int,BlueprintBook> books = new Dictionary<int, BlueprintBook>();
        private static string saveFile = $"{Application.persistentDataPath}/BlueprintBooks.json";

        // Public Functions

        public static int AddBook(BlueprintBook book, bool shouldSave = true) {
            if (book.id == -1) book.id = GetNewBookID();
            books.Add(book.id, book);
            if (shouldSave) SaveData();

            return book.id;
        }

        public static int AddSharableBook(SharableBook book, int parentId = -10) {
            BlueprintBook newBook = new BlueprintBook() {
                name = book.name,
                icon = book.icon,
                description = book.description
            };
            newBook.id = GetNewBookID();
            if(parentId != -10) newBook.parentId = parentId;
            AddBook(newBook);

            foreach(SharableSlot slot in book.slots) {
                if(slot.blueprint != null) {
                    slot.blueprint.id = 01;
                    slot.blueprint.parentId = newBook.id;
                    int id = BlueprintManager.AddBlueprint(slot.blueprint);
                    newBook.AddBlueprint(id);
                }
                else if (slot.sharableBook != null) {
                    int childBookID = AddSharableBook(slot.sharableBook, newBook.id);
                    newBook.AddBook(childBookID);
                }
            }

            UpdateBook(newBook);
            return newBook.id;
        }

        public static void UpdateBook(BlueprintBook book) {
            if (DoesBookExist(book)) {
                books[book.id] = book;
                SaveData();
            }
        }

        public static bool DoesBookExist(int id) {
            return books.ContainsKey(id);
        }

        public static int GetBookCount() {
            return books.Count;
        }

        public static BlueprintBook TryGetBook(int id) {
            if (DoesBookExist(id)) return books[id];
            else return null;
        }

        public static List<BlueprintBook> GetAllBooks() {
            return books.Values.ToList();
        }

        public static BlueprintBook GetRootBook() {
            return books[0];
        }

        public static BlueprintBook GetCurrentBook() {
            return books[currentBookId];
        }

        public static void DeleteBook(BlueprintBook book, bool removeFromParent = false) {
            if (book == null) return;
            if (!DoesBookExist(book.id)) return;

            if(DoesBookExist(book.parentId) && removeFromParent) {
                BlueprintBook parent = TryGetBook(book.parentId);
                parent.RemoveBook(book.id);
            }

            foreach(Slot slot in book.slots) {
                if (slot.GetSlotType() == SlotType.Blueprint) BlueprintManager.DeleteBlueprint(slot.blueprintId);
                else DeleteBook(slot.bookId);
            }

            books.Remove(book.id);

            SaveData();
        }

        // Private Functions

        private static int GetNewBookID() {
            if (books.Count == 0) return 0;
            else return books.Keys.Max() + 1;
        }

        // Data Functions

        public static void SaveData() {
            List<string> jsons = new List<string>();
            foreach(BlueprintBook book in books.Values) {
                jsons.Add(book.ToJson());
            }

            File.WriteAllLines(saveFile, jsons);
        }

        public static void LoadData() {
            if (!File.Exists(saveFile)) {
                BlueprintsPlugin.Log.LogWarning("BlueprintBooks.json not found");
                return;
            }

            string[] jsons = File.ReadAllLines(saveFile);
            foreach(string json in jsons) {
                AddBook((BlueprintBook)JsonUtility.FromJson(json, typeof(BlueprintBook)), false);
            }
        }

        #region Overloads

        public static bool DoesBookExist(BlueprintBook book) {
            return DoesBookExist(book.id);
        }

        public static void DeleteBook(int id) {
            DeleteBook(TryGetBook(id));
        }

        #endregion
    }
}
