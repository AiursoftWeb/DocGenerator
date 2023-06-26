using Aiursoft.DocGenerator.Attributes;
using Aiursoft.DocGenerator.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Aiursoft.DocGenerator.Tests.Services
{
    [TestClass]
    public class InstanceMakerTests
    {
        [TestMethod]
        public void Make_String_ReturnsExampleString()
        {
            // Arrange
            var instanceMaker = new InstanceMaker();
            
            // Act
            var result = instanceMaker.Make(typeof(string));
            
            // Assert
            Assert.AreEqual("an example string.", result);
        }
        
        [TestMethod]
        public void Make_Int_ReturnsZero()
        {
            // Arrange
            var instanceMaker = new InstanceMaker();
            
            // Act
            var result = instanceMaker.Make(typeof(int));
            
            // Assert
            Assert.AreEqual(0, result);
        }
        
        [TestMethod]
        public void Make_NullableInt_ReturnsZero()
        {
            // Arrange
            var instanceMaker = new InstanceMaker();
            
            // Act
            var result = instanceMaker.Make(typeof(int?));
            
            // Assert
            Assert.AreEqual(0, result);
        }
        
        [TestMethod]
        public void Make_DateTime_ReturnsCurrentDateTime()
        {
            // Arrange
            var instanceMaker = new InstanceMaker();
            
            // Act
            var result = instanceMaker.Make(typeof(DateTime));
            
            // Assert
            Assert.IsTrue(result is DateTime);
        }
        
        [TestMethod]
        public void Make_NullableDateTime_ReturnsCurrentDateTime()
        {
            // Arrange
            var instanceMaker = new InstanceMaker();
            
            // Act
            var result = instanceMaker.Make(typeof(DateTime?));
            
            // Assert
            Assert.IsTrue(result is DateTime);
        }
        
        [TestMethod]
        public void Make_Guid_ReturnsNewGuid()
        {
            // Arrange
            var instanceMaker = new InstanceMaker();
            
            // Act
            var result = instanceMaker.Make(typeof(Guid));
            
            // Assert
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(Guid));
        }
        
        [TestMethod]
        public void Make_NullableGuid_ReturnsNewGuid()
        {
            // Arrange
            var instanceMaker = new InstanceMaker();
            
            // Act
            var result = instanceMaker.Make(typeof(Guid?));
            
            // Assert
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(Guid));
        }
        
        [TestMethod]
        public void Make_DateTimeOffset_ReturnsCurrentDateTimeOffset()
        {
            // Arrange
            var instanceMaker = new InstanceMaker();
            
            // Act
            var result = instanceMaker.Make(typeof(DateTimeOffset));
            
            // Assert
            Assert.IsTrue(result is DateTimeOffset);
        }
        
        [TestMethod]
        public void Make_NullableDateTimeOffset_ReturnsCurrentDateTimeOffset()
        {
            // Arrange
            var instanceMaker = new InstanceMaker();
            
            // Act
            var result = instanceMaker.Make(typeof(DateTimeOffset?));
            
            // Assert
            Assert.IsTrue(result is DateTimeOffset);
        }
        
        [TestMethod]
        public void Make_TimeSpan_ReturnsTimeSpanFromMinutes()
        {
            // Arrange
            var instanceMaker = new InstanceMaker();
            
            // Act
            var result = instanceMaker.Make(typeof(TimeSpan));
            
            // Assert
            Assert.AreEqual(TimeSpan.FromMinutes(37), result);
        }
        
        [TestMethod]
        public void Make_NullableTimeSpan_ReturnsTimeSpanFromMinutes()
        {
            // Arrange
            var instanceMaker = new InstanceMaker();
            
            // Act
            var result = instanceMaker.Make(typeof(TimeSpan?));
            
            // Assert
            Assert.AreEqual(TimeSpan.FromMinutes(37), result);
        }
        
        [TestMethod]
        public void Make_Bool_ReturnsTrue()
        {
            // Arrange
            var instanceMaker = new InstanceMaker();
            
            // Act
            var result = instanceMaker.Make(typeof(bool));
            
            // Assert
            Assert.AreEqual(true, result);
        }
        
        [TestMethod]
        public void Make_NullableBool_ReturnsTrue()
        {
            // Arrange
            var instanceMaker = new InstanceMaker();
            
            // Act
            var result = instanceMaker.Make(typeof(bool?));
            
            // Assert
            Assert.AreEqual(true, result);
        }
        
        [TestMethod]
        public void Make_ComplicatedObject()
        {
            // Arrange
            var instanceMaker = new InstanceMaker();
            
            // Act
            var result = instanceMaker.Make(typeof(Major));
            
            // Assert
            Assert.IsTrue((result as Major)?.RequiresCollege);
            Assert.IsTrue((result as Major)?.TextBooks.Any());
            Assert.IsTrue((result as Major)?.TestBooks.Any());
            Assert.IsTrue((result as Major)?.TestBooks.First().Author.Length > 1);

        }
    }
}

public class Major
{
    public Major(
        string majorName, 
        bool requiresCollege, 
        Guid id, 
        DateTime createTime, 
        List<SampleBook> textBooks, 
        SampleBook[] testBooks, 
        List<Teacher> teachers, 
        TimeSpan requiresTeaching)
    {
        MajorName = majorName;
        RequiresCollege = requiresCollege;
        Id = id;
        CreateTime = createTime;
        TextBooks = textBooks;
        TestBooks = testBooks;
        Teachers = teachers;
        RequiresTeaching = requiresTeaching;
    }

    public string MajorName { get; set; }
    public bool RequiresCollege { get; set; }
    public Guid Id { get; set; }
    public DateTime CreateTime { get; set; }
    public TimeSpan RequiresTeaching { get; set; }
    
    public List<Teacher> Teachers { get; set; }
    public List<SampleBook> TextBooks { get; set; }
    public SampleBook[] TestBooks { get; set; }
}

public class SampleBook
{
    public SampleBook(string title, string author, DateTime releaseYear)
    {
        Title = title;
        Author = author;
        ReleaseYear = releaseYear;
    }

    public string Title { get; set; }
    public string Author { get; set; }
    
    public DateTime ReleaseYear { get; set; }
    
    [InstanceMakerIgnore]
    public SampleBook[] ReferencedBy = Array.Empty<SampleBook>();
}

public abstract class Teacher
{
    
}