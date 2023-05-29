﻿using System.Linq.Expressions;
using Flurl.Http;
using Moments.Model;

namespace Moments.Service;

/// <summary>
/// 朋友服务
/// </summary>
public class FriendService
{
    private readonly IFreeSql _db;
    private readonly ConfigService _configService;

    public FriendService(IFreeSql db, ConfigService configService)
    {
        _db = db;
        _configService = configService;
    }

    /// <summary>
    /// 获取朋友总数
    /// </summary>
    /// <param name="isVis">筛选可见性</param>
    /// <returns></returns>
    public long Count(bool? isVis = null)
    {
        return isVis is not null
            ? _db.Select<Friend>().Where(x => x.Visible == isVis).Count()
            : _db.Select<Friend>().Count();
    }

    /// <summary>
    /// 获取一个朋友
    /// </summary>
    /// <param name="exp">过滤表达式</param>
    /// <returns></returns>
    public Friend? First(Expression<Func<Friend, bool>> exp)
    {
        return _db.Select<Friend>().Where(exp).First();
    }

    /// <summary>
    /// 获取所有朋友
    /// </summary>
    /// <param name="isVis">筛选可见性</param>
    /// <returns></returns>
    public List<Friend> List(bool? isVis = null)
    {
        return isVis is not null
            ? _db.Select<Friend>().Where(x => x.Visible == isVis).ToList()
            : _db.Select<Friend>().ToList();
    }

    /// <summary>
    /// 异步获取所有朋友
    /// </summary>
    /// <param name="isVis">筛选可见性</param>
    /// <returns></returns>
    public async Task<List<Friend>> ListAsync(bool? isVis = null)
    {
        return isVis is not null
            ? await _db.Select<Friend>().Where(x => x.Visible == isVis).ToListAsync()
            : await _db.Select<Friend>().ToListAsync();
    }


    /// <summary>
    /// 增加朋友记录
    /// </summary>
    /// <param name="item">朋友</param>
    /// <returns>是否添加成功</returns>
    public bool Add(Friend item)
    {
        var rows = _db.Insert<Friend>()
            .AppendData(item)
            .ExecuteAffrows();
        return rows > 0;
    }

    /// <summary>
    /// 编辑朋友
    /// </summary>
    /// <param name="item">朋友</param>
    /// <returns></returns>
    public bool Edit(Friend item)
    {
        var rows = _db.Update<Friend>()
            .SetSource(item)
            .ExecuteAffrows();
        return rows > 0;
    }

    public bool Del(Friend item)
    {
        var rows = _db.Delete<Friend>()
            .Where(x => x.FriendId == item.FriendId)
            .ExecuteAffrows();
        return rows > 0;
    }

    /// <summary>
    /// 验证朋友站点是否存在本站链接
    /// </summary>
    /// <param name="target"></param>
    public async Task<bool> VerifyItem(Friend target)
    {
        if (target.VerifyUrl is not null)
        {
            try
            {
                await target.VerifyUrl.GetStringAsync();
            }
            catch
            {
                await _db.Update<Friend>().Where(x => x.FriendId == target.FriendId)
                    .Set(x => x.Verify, FriendState.无法访问).ExecuteAffrowsAsync();
                return false;
            }

            var temp = await target.VerifyUrl.GetStringAsync();
            if (temp.IndexOf(_configService.Get("Origin"), StringComparison.Ordinal) != -1)
            {
                await _db.Update<Friend>().Where(x => x.FriendId == target.FriendId).Set(x => x.Verify, FriendState.正常)
                    .ExecuteAffrowsAsync();
                return true;
            }
            else
            {
                await _db.Update<Friend>().Where(x => x.FriendId == target.FriendId).Set(x => x.Verify, FriendState.失链)
                    .ExecuteAffrowsAsync();
                return false;
            }
        }

        return false;
    }

    /// <summary>
    /// 设置朋友的可见性
    /// </summary>
    /// <param name="friendId">朋友编号</param>
    /// <param name="vis">可见性</param>
    /// <returns></returns>
    public async Task<bool> SetVis(int friendId, bool vis)
    {
        var row = await _db.Update<Friend>().Where(x => x.FriendId == friendId).Set(x => x.Visible, vis)
            .ExecuteAffrowsAsync();
        return row > 0;
    }
}