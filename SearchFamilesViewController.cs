using System;
using UIKit;
using System.IO;
using Foundation;
using Rock.Mobile.PlatformSpecific.iOS.UI;
using CoreGraphics;
using Rock.Mobile.Util.Strings;
using Rock.Mobile.IO;
using FamilyManager.UI;
using Rock.Mobile.PlatformSpecific.Util;
using System.Collections.Generic;
using iOS;
using Customization;
using Rock.Mobile.PlatformSpecific.iOS.Graphics;
using System.Linq;
using Rock.Mobile.Network;
using Rock.Mobile.Util;

namespace FamilyManager
{
    public class SearchFamiliesViewController : UIViewController
    {
        /// <summary>
        /// Definition for a cell that can be used to display family search results
        /// </summary>
        class FamilySearchResultCell : UITableViewCell
        {
            public static string Identifier = "FamilySearchResultCell";

            public FamilySearchResultView FamilyView { get; set; }

            public FamilySearchResultCell( UITableViewCellStyle style, string cellIdentifier ) : base( style, cellIdentifier )
            {
                BackgroundColor = UIColor.Clear;
                FamilyView = new FamilySearchResultView( );

                AddSubview( FamilyView );
            }
        }

        public class TableSource : UITableViewSource 
        {
            SearchFamiliesViewController Parent { get; set; }

            List<nfloat> CellHeights { get; set; }
            static nfloat FooterCellHeight = 100;

            /// <summary>
            /// Definition for the source table that backs the tableView
            /// </summary>
            public TableSource ( SearchFamiliesViewController parent )
            {
                Parent = parent;

                // create a list for the cell heights, since they're variable
                CellHeights = new List<nfloat>( );
            }

            public void FamiliesUpdated( UITableView parentTable )
            {
                CellHeights = new List<nfloat>();
            }

            public override nint RowsInSection (UITableView tableview, nint section)
            {
                // IF there's at least one family, add one extra row for padding. Otherwise don't show anything.
                return Parent.Families.Count > 0 ? Parent.Families.Count + 1 : 1;
            }

            public override void RowSelected (UITableView tableView, NSIndexPath indexPath)
            {
                tableView.DeselectRow( indexPath, true );

                if ( indexPath.Row < Parent.Families.Count )
                {
                    Parent.RowClicked( indexPath.Row );
                }
            }

            public override nfloat GetHeightForRow(UITableView tableView, NSIndexPath indexPath)
            {
                return CalcRowHeight( tableView, indexPath.Row );
            }

            public override nfloat EstimatedHeight(UITableView tableView, NSIndexPath indexPath)
            {
                return CalcRowHeight( tableView, indexPath.Row );
            }

            nfloat CalcRowHeight( UITableView tableView, int rowIndex )
            {
                if ( rowIndex < CellHeights.Count )
                {
                    return CellHeights[ rowIndex ];
                }
                else
                {
                    return FooterCellHeight;
                }
            }

            public override UITableViewCell GetCell (UITableView tableView, NSIndexPath indexPath)
            {
                FamilySearchResultCell cell = tableView.DequeueReusableCell( FamilySearchResultCell.Identifier ) as FamilySearchResultCell;

                // if there are no cells to reuse, create a new one
                if (cell == null)
                {
                    cell = new FamilySearchResultCell( UITableViewCellStyle.Default, FamilySearchResultCell.Identifier );

                    // configure the cell colors
                    cell.SelectionStyle = UITableViewCellSelectionStyle.None;
                }

                nfloat cellWidth = tableView.Bounds.Width * .98f;

                if ( Parent.Families.Count > 0 )
                {
                    // is this an actual family row? (as opposed to our bottom dummy row)
                    if ( indexPath.Row < Parent.Families.Count )
                    {
                        cell.FamilyView.FormatCell( cellWidth, Parent.Families[ indexPath.Row ] );

                        cell.Bounds = cell.FamilyView.Bounds;

                        // if the row is beyond what we've stored, add it.
                        if ( indexPath.Row >= CellHeights.Count )
                        {
                            CellHeights.Add( cell.Bounds.Height );
                        }
                        else
                        {
                            // otherwise replace the existing entry
                            CellHeights[ indexPath.Row ] = cell.Bounds.Height;
                        }
                    }
                    else
                    {
                        // this is the bottom dummy cell, so just give it a little padding.
                        cell.FamilyView.Container.Hidden = true;
                    }
                }
                // either they've never searched, or it's a failed search
                else
                {
                    if ( Parent.DidSearchFail == true )
                    {
                        cell.FamilyView.FormatCell( cellWidth, Strings.Search_NoResults_Title, Strings.Search_NoResults_Suggestions, "", Strings.Search_NoResults_Suggestion1, Strings.Search_NoResults_Suggestion2 );

                        // if the row is beyond what we've stored, add it.
                        if ( indexPath.Row >= CellHeights.Count )
                        {
                            CellHeights.Add( cell.Bounds.Height );
                        }
                        else
                        {
                            // otherwise replace the existing entry
                            CellHeights[ indexPath.Row ] = cell.Bounds.Height;
                        }
                    }
                    else
                    {
                        cell.FamilyView.Container.Hidden = true;
                    }
                }

                return cell;
            }
        }

        Dynamic_UITextField SearchField { get; set; }
        UIButton SearchButton { get; set; }
        UIButton ClearButton { get; set; }

        UIButton AddFamilyButton { get; set; }

        UITableView TableView { get; set; }

        UIBlockerView BlockerView { get; set; }

        ContainerViewController Parent { get; set; }

        public List<Rock.Client.Family> Families { get; set; }

        public bool DidSearchFail { get; set; }


        public SearchFamiliesViewController( ContainerViewController parent ) : base( )
        {
            Parent = parent;
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            View.Layer.Contents = Parent.View.Layer.Contents;

            Families = new List<Rock.Client.Family>();

            // setup the blocker view
            BlockerView = new UIBlockerView( View, View.Bounds.ToRectF( ) );


            // setup the search field
            SearchField = new Dynamic_UITextField( this, View, Strings.General_Search, false, false );
            View.AddSubview( SearchField );

            SearchField.GetTextField( ).AutocapitalizationType = UITextAutocapitalizationType.None;
            SearchField.GetTextField( ).AutocorrectionType = UITextAutocorrectionType.No;
            SearchField.GetTextField( ).ClearButtonMode = UITextFieldViewMode.Always;

            SearchField.GetTextField( ).ShouldReturn += delegate(UITextField textField) 
                {
                    PerformSearch( );
                    return true;
                };
            
            DidSearchFail = false;

            // setup the search button
            SearchButton = UIButton.FromType( UIButtonType.System );
            SearchButton.Layer.AnchorPoint = CGPoint.Empty;
            SearchButton.SetTitle( Strings.General_Search, UIControlState.Normal );
            Theme.StyleButton( SearchButton, Config.Instance.VisualSettings.PrimaryButtonStyle );
            SearchButton.Font = FontManager.GetFont( Settings.General_RegularFont, Config.Instance.VisualSettings.SmallFontSize );
            View.AddSubview( SearchButton );

            SearchButton.TouchUpInside += (object sender, EventArgs e ) =>
                {
                    PerformSearch( );
                };


            // setup the refresh button
            ClearButton = UIButton.FromType( UIButtonType.System );
            ClearButton.Layer.AnchorPoint = CGPoint.Empty;
            ClearButton.SetTitle( Strings.General_Clear, UIControlState.Normal );
            Theme.StyleButton( ClearButton, Config.Instance.VisualSettings.DefaultButtonStyle );
            View.AddSubview( ClearButton );
            ClearButton.TouchUpInside += (object sender, EventArgs e ) =>
                {
                    Families = new List<Rock.Client.Family>( );
                    DidSearchFail = false;

                    // reload data
                    ((TableSource)TableView.Source).FamiliesUpdated( TableView );
                    TableView.ReloadData( );
                };


            // setup the add family button
            AddFamilyButton = UIButton.FromType( UIButtonType.System );
            AddFamilyButton.Layer.AnchorPoint = CGPoint.Empty;
            AddFamilyButton.SetTitle( Strings.General_AddFamily, UIControlState.Normal );
            AddFamilyButton.Font = FontManager.GetFont( Settings.General_RegularFont, Config.Instance.VisualSettings.SmallFontSize );
            Theme.StyleButton( AddFamilyButton, Config.Instance.VisualSettings.DefaultButtonStyle );

            View.AddSubview( AddFamilyButton );

            AddFamilyButton.TouchUpInside += (object sender, EventArgs e ) =>
            {
                Parent.PresentNewFamilyPage( );
            };


            // setup the list
            TableView = new UITableView( );
            TableView.Layer.AnchorPoint = CGPoint.Empty;
            TableView.BackgroundColor = UIColor.Clear;
            TableView.SeparatorStyle = UITableViewCellSeparatorStyle.None;
            View.AddSubview( TableView );
            TableView.Source = new TableSource( this );
        }

        public void PerformSearch( )
        {
            if( SearchField.GetCurrentValue( ).Length > Settings.General_MinSearchLength )
            {
                // put the list at the top, so that when we refresh it, it correctly resizes each row
                TableView.SetContentOffset( CGPoint.Empty, true );

                SearchField.ResignFirstResponder( );

                BlockerView.BringToFront( );

                BlockerView.Show( 
                    delegate 
                    {
                        // search for the family
                        RockApi.Get_Groups_FamiliesByPersonNameSearch( SearchField.GetCurrentValue( ), 
                            delegate(System.Net.HttpStatusCode statusCode, string statusDescription, List<Rock.Client.Family> model) 
                            {
                                if( Rock.Mobile.Network.Util.StatusInSuccessRange( statusCode ) == true && model != null && model.Count > 0 )
                                {
                                    Families = model;
                                }
                                else
                                {
                                    // error (or no results)
                                    Families = new List<Rock.Client.Family>( );

                                    DidSearchFail = true;
                                }

                                // reload data
                                ((TableSource)TableView.Source).FamiliesUpdated( TableView );
                                TableView.ReloadData( );

                                BlockerView.Hide( );
                            });
                    });
            }
        }

        public override void ViewDidLayoutSubviews()
        {
            base.ViewDidLayoutSubviews();

            // set the search field and refresh button, cause those are known
            SearchField.Layer.Position = new CGPoint( 10, 25 );
            SearchField.ViewDidLayoutSubviews( new CGRect( 0, 0, View.Bounds.Width * 0.3375f, View.Bounds.Height ) );

            // position the clear button
            ClearButton.SizeToFit( );
            ClearButton.Bounds = new CGRect( 0, 0, ClearButton.Bounds.Width * 2, SearchButton.Bounds.Height );
            ClearButton.Layer.Position = new CGPoint( SearchField.Frame.Right - ClearButton.Bounds.Width, SearchField.Frame.Bottom + 10 );

            // now set the search button, which will use remaining width
            SearchButton.Layer.Position = new CGPoint( 10, SearchField.Frame.Bottom + 10 );
            SearchButton.Bounds = new CGRect( 0, 0, SearchField.Bounds.Width, 0 );
            SearchButton.SizeToFit( );
            SearchButton.Frame = new CGRect( 10, SearchField.Frame.Bottom + 10, SearchField.Bounds.Width - ClearButton.Bounds.Width - 2, SearchButton.Bounds.Height );


            // position the 'add family' button
            AddFamilyButton.Bounds = new CGRect( 0, 0, SearchField.Bounds.Width, SearchButton.Bounds.Height );
            AddFamilyButton.Layer.Position = new CGPoint( SearchButton.Frame.Left, SearchButton.Frame.Bottom + 10 );


            // setup the table view
            nfloat tableXPos = SearchField.Frame.Right + 10;
            TableView.Frame = new CGRect( tableXPos, SearchField.Layer.Position.Y, View.Bounds.Width - tableXPos, View.Bounds.Height );
            TableView.SetNeedsLayout( );

            BlockerView.SetBounds( View.Bounds.ToRectF( ) );
        }

        public override void ViewWillAppear( bool animated )
        {
            base.ViewWillAppear( animated );

            TableView.ReloadData( );
        }

        public override void TouchesEnded(NSSet touches, UIEvent evt)
        {
            base.TouchesEnded(touches, evt);

            SearchField.ResignFirstResponder( );
        }

        public void TryUpdateFamily( Rock.Client.Family family )
        {
            // see if the family exists
            Rock.Client.Family currFamily = Families.Where( f => f.Id == family.Id ).SingleOrDefault( );

            // if it does, get its index and replace it.
            if ( currFamily != null )
            {
                int currIndex = Families.IndexOf( currFamily );
                Families[ currIndex ] = family;
            }
        }

        public void RowClicked( int index )
        {
            // notify our parent it should present the family page.
            Parent.PresentFamilyPage( Families[ index ] );
        }
    }
}
