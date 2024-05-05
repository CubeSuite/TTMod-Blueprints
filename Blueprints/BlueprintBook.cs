using EquinoxsModUtils;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Blueprints
{
    [Serializable]
    public class BlueprintBook
    {
        public int id;
        public int parentId = -1;
        public string name = "New Blueprint Book";
        public string icon;
        public string description;
        public List<Slot> slots = new List<Slot>();

        // Public Functions

        public void AddBlueprint(int blueprintId) {
            slots.Add(new Slot() { blueprintId = blueprintId });
        }

        public void RemoveBlueprint(int blueprintId) {
            List<Slot> matchingSlots = slots.Where(thisSlot => thisSlot.blueprintId == blueprintId).ToList();
            if (matchingSlots.Count == 1) {
                slots.Remove(matchingSlots[0]);
            }
        }

        public void AddBook(int bookId) {
            slots.Add(new Slot() { bookId = bookId });
        }

        public void RemoveBook(int bookId) {
            List<Slot> matchingSlots = slots.Where(thisSlot => thisSlot.bookId == bookId).ToList();
            if (matchingSlots.Count == 1) {
                slots.Remove(matchingSlots[0]);
            }
        }

        public BlueprintBook GetParent() {
            return BookManager.TryGetBook(parentId);
        }

        public string GetPath() {
            List<string> names = new List<string>();
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

        public SlotType GetSlotType() {
            if (blueprintId != -1) return SlotType.Blueprint;
            else if (bookId != -1) return SlotType.Book;
            else return SlotType.None;
        }
    }

    public enum SlotType {
        None,
        Blueprint,
        Book
    }
}
