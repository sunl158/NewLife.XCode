﻿using System;
using System.Linq;
using System.ComponentModel;
using System.Web.Mvc;
using NewLife.Cube.Controllers;
using XCode.Membership;
using System.Collections.Generic;

namespace NewLife.Cube.Admin.Controllers
{
    /// <summary>角色控制器</summary>
    [DisplayName("角色")]
    public class RoleController : EntityController<Role>
    {
        /// <summary>保存</summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public override ActionResult Save(Role entity)
        {
            // 保存权限项
            var menus = Menu.Root.AllChilds;
            var pfs = EnumHelper.GetDescriptions<PermissionFlags>().Where(e => e.Key > PermissionFlags.None);
            var dels = new List<Int32>();
            // 遍历所有权限资源
            foreach (var item in menus)
            {
                // 是否授权该项
                var has = Request["p" + item.ID].ToBoolean();
                if (!has)
                    dels.Add(item.ID);
                else
                {
                    // 遍历所有权限子项
                    var any = false;
                    foreach (var pf in pfs)
                    {
                        var has2 = Request["pf" + item.ID + "_" + ((Int32)pf.Key)].ToBoolean();

                        entity.Set(item.ID, has2 ? pf.Key : PermissionFlags.None);
                        any |= has2;
                    }
                    // 如果原来没有权限，这是首次授权，且右边没有勾选任何子项，则授权全部
                    if (!any & !entity.Has(item.ID)) entity.Set(item.ID);
                }
            }
            // 删除已经被放弃权限的项
            foreach (var item in dels)
            {
                if (entity.Has(item)) entity.Permissions.Remove(item);
            }

            return base.Save(entity);
        }
    }
}