using EquinoxsModUtils;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.XPath;
using UnityEngine;

namespace Blueprints
{
    [Serializable]
    public class BlueprintBook
    {
        public int id = -1;
        public int parentId = -1;
        public string name = "New Book";
        public string icon;
        public string description;
        public List<string> slots = new List<string>();
        
        // public List<Slot> slots = new List<Slot>();

        // Public Functions

        public void AddBlueprint(int blueprintId) {
            slots.Add(new Slot() { blueprintId = blueprintId }.ToString());
        }

        public void RemoveBlueprint(int blueprintId) {
            for (int i = 0; i < slots.Count; i++) {
                if (slots[i].Split(',')[0] == blueprintId.ToString()) {
                    slots.RemoveAt(i);
                    return;
                }
            }
        }

        public void AddBook(int bookId) {
            slots.Add(new Slot() { bookId = bookId }.ToString());
        }

        public void RemoveBook(int bookId) {
            for (int i = 0; i < slots.Count; i++) {
                if (slots[i].Split(',')[1] == bookId.ToString()) {
                    slots.RemoveAt(i);
                    return;
                }
            }
        }

        public List<Slot> GetSlots() {
            List<Slot> results = new List<Slot>();
            foreach(string slot in slots) {
                results.Add(new Slot(slot));
            }

            return results;
        }

        public BlueprintBook GetParent() {
            return BookManager.TryGetBook(parentId);
        }

        public string GetPath() {
            List<string> names = new List<string>() { name };
            BlueprintBook currentBook = this;
            while(currentBook.parentId != -1) {
                BlueprintBook parent = currentBook.GetParent();
                names.Insert(0, parent.name);
                currentBook = parent;
            }

            return string.Join(" > ", names);
        }

        public string ToJson(bool formatted = false) {
            return JsonUtility.ToJson(this, formatted);
        }

        #region OverLoads

        public void AddBlueprint(Blueprint blueprint) {
            AddBlueprint(blueprint.id);
        }

        public void RemoveBlueprint(Blueprint blueprint) {
            RemoveBlueprint(blueprint.id);
        }

        public void AddBook(BlueprintBook book) {
            AddBook(book.id);
        }

        public void RemoveBook(BlueprintBook book) {
            RemoveBook(book.id);
        }

        #endregion
    }

    [Serializable]
    public class Slot {
        public int blueprintId = -1;
        public int bookId = -1;

        // Public Functions

        public SlotType GetSlotType() {
            if (blueprintId != -1) return SlotType.Blueprint;
            else if (bookId != -1) return SlotType.Book;
            else return SlotType.None;
        }

        public override string ToString() {
            return $"{blueprintId},{bookId}";
        }

        // Constructors

        public Slot(){}
        public Slot(string input) {
            string[] idStrings = input.Split(',');
            blueprintId = int.Parse(idStrings[0]);
            bookId = int.Parse(idStrings[1]);
        }
    }

    public enum SlotType {
        None,
        Blueprint,
        Book
    }
}
