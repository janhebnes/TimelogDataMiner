USE [DataWarehouse]
GO
/****** Object:  Table [dbo].[TimeLogEmployee]    Script Date: 09-12-2015 16:52:52 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[TimeLogEmployee](
	[ID] [int] NOT NULL,
	[FirstName] [nvarchar](100) NULL,
	[LastName] [nvarchar](100) NULL,
	[FullName] [nvarchar](100) NULL,
	[Initials] [nvarchar](100) NULL,
	[Title] [nvarchar](100) NULL,
	[Email] [nvarchar](50) NULL,
	[Status] [int] NULL,
	[DepartmentNameId] [int] NULL,
	[DepartmentName] [nvarchar](100) NULL,
 CONSTRAINT [PK_Employee] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
