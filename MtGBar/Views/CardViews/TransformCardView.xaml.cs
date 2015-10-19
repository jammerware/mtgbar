﻿using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Melek.Domain;

namespace MtGBar.Views.CardViews
{
    public partial class TransformCardView : UserControl
    {
        public TransformCardView()
        {
            InitializeComponent();
            LayoutRoot.DataContext = this;
        }
        
        public TransformCard Card
        {
            get { return (TransformCard)GetValue(CardProperty); }
            set { SetValue(CardProperty, value); }
        }
        
        public static readonly DependencyProperty CardProperty = DependencyProperty.Register(
            "Card", 
            typeof(TransformCard), 
            typeof(TransformCardView), 
            new PropertyMetadata(null)
        );
        
        public TransformPrinting Printing
        {
            get { return (TransformPrinting)GetValue(PrintingProperty); }
            set { SetValue(PrintingProperty, value); }
        }
        
        public static readonly DependencyProperty PrintingProperty = DependencyProperty.Register(
            "Printing", 
            typeof(TransformPrinting), 
            typeof(TransformCardView), 
            new PropertyMetadata(null)
        );
        
        public BitmapImage NormalImage
        {
            get { return (BitmapImage)GetValue(NormalImageProperty); }
            set { SetValue(NormalImageProperty, value); }
        }
        
        public static readonly DependencyProperty NormalImageProperty = DependencyProperty.Register(
            "NormalImage", 
            typeof(BitmapImage), 
            typeof(TransformCardView), 
            new PropertyMetadata(null)
        );

        public BitmapImage TransformedImage
        {
            get { return (BitmapImage)GetValue(TransformedImageProperty); }
            set { SetValue(TransformedImageProperty, value); }
        }

        public static readonly DependencyProperty TransformedImageProperty = DependencyProperty.Register(
            "TransformedImage", 
            typeof(BitmapImage), 
            typeof(TransformCardView), 
            new PropertyMetadata(null)
        );
    }
}