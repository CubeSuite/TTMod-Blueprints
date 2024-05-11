using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using UnityEngine;

namespace Blueprints
{
    [Serializable]
    public class SharableBook
    {
        public string name;
        public string icon;
        public string description;
        public List<SharableSlot> slots = new List<SharableSlot>();

        // Constructors

        public SharableBook(){}
        public SharableBook(BlueprintBook book) {
            name = string.IsNullOrEmpty(book.name) ? "null" : book.name;
            icon = string.IsNullOrEmpty(book.icon) ? "null" : book.icon;
            description = string.IsNullOrEmpty(book.description) ? "null" : book.description;
            foreach(Slot slot in book.GetSlots()) {
                slots.Add(new SharableSlot(slot));
            }
        }

        public static SharableBook Parse(string input) {
            XmlSerializer serialiser = new XmlSerializer(typeof(SharableBook));
            using (TextReader reader = new StringReader(input)) {
                return (SharableBook)serialiser.Deserialize(reader);
            }
        }

        // Public Functions

        public string Serialise() {
            XmlSerializer serialiser = new XmlSerializer(typeof(SharableBook));
            using(StringWriter writer = new StringWriter()) {
                serialiser.Serialize(writer, this);
                return writer.ToString();
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
