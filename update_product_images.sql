-- Script để update hình ảnh cho tất cả sản phẩm
-- Gán random các file ảnh khác nhau cho mỗi sản phẩm

DECLARE @images TABLE (Id INT IDENTITY(1,1), ImageName NVARCHAR(200));
INSERT INTO @images (ImageName) VALUES
('bia-sach2-9886.jpg'),
('download (1).jpg'),
('download (2).jpg'),
('download (3).jpg'),
('download (4).jpg'),
('download (5).jpg'),
('download (6).jpg'),
('download (7).jpg'),
('download (8).jpg'),
('download (9).jpg'),
('download (10).jpg'),
('download (11).jpg'),
('download (12).jpg'),
('download (13).jpg'),
('download (14).jpg'),
('download (15).jpg'),
('download (16).jpg'),
('download.jpg'),
('download.png'),
('images.jpg');

DECLARE @imageCount INT = (SELECT COUNT(*) FROM @images);

IF @imageCount = 0
BEGIN
    RAISERROR('Không có ảnh trong thư mục AnhBia', 16, 1);
    RETURN;
END

-- Update từng sản phẩm với hình ảnh khác nhau
;WITH ProductsWithRowNum AS (
    SELECT ProductID, ROW_NUMBER() OVER (ORDER BY NEWID()) AS RowNum
    FROM Products
)
UPDATE p
SET Image = CONCAT('AnhBia/', i.ImageName)
FROM Products p
INNER JOIN ProductsWithRowNum pr ON p.ProductID = pr.ProductID
INNER JOIN @images i ON ((pr.RowNum - 1) % @imageCount) + 1 = i.Id;

SELECT COUNT(*) AS 'Số sản phẩm đã update' FROM Products WHERE Image IS NOT NULL;
