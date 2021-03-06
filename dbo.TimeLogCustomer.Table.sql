USE [DataWarehouse]
GO
/****** Object:  Table [dbo].[TimeLogCustomer]    Script Date: 09-12-2015 16:52:52 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[TimeLogCustomer](
	[ID] [int] NOT NULL,
	[Name] [nvarchar](100) NULL,
	[No] [nvarchar](100) NULL,
	[CustomerStatusID] [nvarchar](100) NULL,
	[CustomerStatus] [nvarchar](100) NULL,
	[Email] [nvarchar](100) NULL,
	[WebPage] [nvarchar](100) NULL,
	[VATNo] [nvarchar](100) NULL,
	[Comment] [nvarchar](800) NULL,
	[IndustryID] [nvarchar](100) NULL,
	[IndustryName] [nvarchar](100) NULL,
 CONSTRAINT [PK_Customer] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
