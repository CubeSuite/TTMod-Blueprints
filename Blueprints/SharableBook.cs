using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Blueprints
{
    [Serializable]
    public class SharableBook
    {
        public string name;
        public string icon;
        public string description;
        public List<SharableSlot> slots = new List<SharableSlot>();

        public SharableBook(){}
        public SharableBook(BlueprintBook book) {
            name = book.name;
            foreach(Slot slot in book.slots) {
                slots.Add(new SharableSlot(slot));
            }
        }
    }

    [Serializable]
    public class SharableSlot {
        public Blueprint blueprint;
        public SharableBook sharableBook;

        public SharableSlot(){}
        public SharableSlot(Slot slot) {
            blueprint = BlueprintManager.TryGetBlueprint(slot.blueprintId);
            BlueprintBook book = BookManager.TryGetBook(slot.bookId);
            if (book != null) sharableBook = new SharableBook(book);
        }
    }
}
