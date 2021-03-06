USE [DataWarehouse]
GO
/****** Object:  Table [dbo].[TimeLogProject]    Script Date: 09-12-2015 16:52:52 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[TimeLogProject](
	[ID] [int] NOT NULL,
	[Name] [nvarchar](200) NULL,
	[No] [nvarchar](100) NULL,
	[Status] [int] NULL,
	[CustomerID] [int] NULL,
	[CustomerName] [nvarchar](100) NULL,
	[CustomerNo] [nvarchar](100) NULL,
	[PMID] [int] NULL,
	[PMInitials] [nvarchar](100) NULL,
	[PMFullName] [nvarchar](100) NULL,
	[ProjectTypeID] [int] NULL,
	[ProjectTypeName] [nvarchar](100) NULL,
	[ProjectCategoryID] [int] NULL,
	[ProjectCategoryName] [nvarchar](100) NULL,
 CONSTRAINT [PK_Project] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
